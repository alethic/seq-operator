using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Alethic.Seq.Operator.ApiKey;
using Alethic.Seq.Operator.Options;

using k8s;
using k8s.Models;

using KubeOps.Abstractions.Controller;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Queue;
using KubeOps.Abstractions.Rbac;
using KubeOps.KubernetesClient;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Seq.Api;
using Seq.Api.Model.Settings;

namespace Alethic.Seq.Operator.Instance
{

    [EntityRbac(typeof(V1alpha1Instance), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(V1Secret), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(V1Service), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(V1ServiceAccount), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(V1StatefulSet), Verbs = RbacVerb.All)]
    [EntityRbac(typeof(V1alpha1ApiKey), Verbs = RbacVerb.All)]
    public partial class V1alpha1InstanceController :
        V1alpha1Controller<V1alpha1Instance, V1alpha1InstanceSpec, V1alpha1InstanceStatus, InstanceConf, InstanceInfo>,
        IEntityController<V1alpha1Instance>
    {

        /// <summary>
        /// Generates a new random password.
        /// </summary>
        /// <returns></returns>
        static string GeneratePassword(int length)
        {
            const string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789";
            const string specialCharacters = @"!#$%&'()*+,-./:;<=>?@[\]_";

            var chars = (Span<char>)stackalloc char[length];

            for (var i = 0; i < length; i++)
            {
                if (i % Random.Shared.Next(3, length) == 0)
                    chars[i] = specialCharacters[Random.Shared.Next(0, specialCharacters.Length)];
                else
                    chars[i] = allowedChars[Random.Shared.Next(0, allowedChars.Length)];
            }

            return new string(chars);
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="kube"></param>
        /// <param name="requeue"></param>
        /// <param name="cache"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public V1alpha1InstanceController(IKubernetesClient kube, EntityRequeue<V1alpha1Instance> requeue, IMemoryCache cache, IOptions<OperatorOptions> options, ILogger<V1alpha1InstanceController> logger) :
            base(kube, requeue, cache, options, logger)
        {

        }

        /// <summary>
        /// Calculates the hash of the password with the given salt.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public string CalculatePasswordHash(V1alpha1Instance instance, string password)
        {
            var instanceNamespace = instance.Namespace();
            var instanceName = instance.Name();

            return Cache.GetOrCreate((typeof(V1alpha1InstanceController), nameof(CalculatePasswordHash), instanceNamespace, instanceName, password), e =>
            {
                var salt = SHA256.HashData(Encoding.UTF8.GetBytes($"{instanceNamespace}/{instanceName}"))[0..16];
                var hash = SeqPasswordHash.Calculate(password, salt);
                var b64e = SeqPasswordHash.ToBase64(hash, salt);
                e.SetAbsoluteExpiration(DateTimeOffset.Now.AddHours(1));
                return b64e;
            })!;
        }

        /// <inheritdoc />
        protected override string EntityTypeName => "Instance";

        /// <inheritdoc />
        protected override async Task<V1alpha1Instance> Reconcile(V1alpha1Instance instance, CancellationToken cancellationToken)
        {
            // deploy locally if no remote specified
            if (instance.Spec.Remote is null)
                await ReconcileDeploymentAsync(instance, instance.Spec.Deployment, cancellationToken);

            // open connection to Seq
            var api = await GetInstanceConnectionAsync(instance, cancellationToken);
            if (api == null)
                throw new InvalidOperationException($"{EntityTypeName} {instance.Namespace()}:{instance.Name()} failed to retrieve API client.");

            // get existing info
            var info = await GetInfoAsync(api, cancellationToken);
            if (info is null)
                throw new InvalidOperationException($"{EntityTypeName} {instance.Namespace()}/{instance.Name()} cannot be loaded from API.");

            // configuration was specified
            if (instance.Spec.Conf is { } conf)
            {
                await PutConfAsync(instance, api, info, conf, cancellationToken);
                info = await GetInfoAsync(api, cancellationToken);
            }

            // retrieve and copy applied settings to status
            instance.Status.Info = info;
            instance = await Kube.UpdateStatusAsync(instance, cancellationToken);
            return instance;
        }

        /// <summary>
        /// Reconciles the deployment information, if present.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="deployment"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<V1alpha1Instance> ReconcileDeploymentAsync(V1alpha1Instance instance, InstanceDeploymentSpec? deployment, CancellationToken cancellationToken)
        {
            deployment ??= new InstanceDeploymentSpec();

            var loginSecret = await ReconcileDeploymentLoginSecretAsync(instance, deployment, cancellationToken);
            var adminApiKey = await ReconcileDeploymentAdminApiKeyAsync(instance, deployment, cancellationToken);
            var serviceAccount = await ReconcileDeploymentServiceAccountAsync(instance, deployment, cancellationToken);
            var service = await ReconcileDeploymentServiceAsync(instance, deployment, cancellationToken);
            var statefulSet = await ReconcileDeploymentStatefulSetAsync(instance, deployment, loginSecret, serviceAccount, service, cancellationToken);

            if (service != null)
            {
                instance.Status.Deployment = new InstanceDeploymentStatus();
                instance.Status.Deployment.Endpoint = $"http://{service.Name()}.{service.Namespace()}.svc.cluster.local:80/";
                instance.Status.Deployment.LoginSecretRef = new V1SecretReference(loginSecret.Name(), loginSecret.Namespace());
                instance.Status.Deployment.TokenSecretRef = adminApiKey?.Status.SecretRef;
                instance = await Kube.SaveAsync(instance, cancellationToken);
            }
            else
            {
                if (instance.Status.Deployment is not null)
                {
                    instance.Status.Deployment = null;
                    instance = await Kube.SaveAsync(instance, cancellationToken);
                }
            }

            return instance;
        }

        /// <summary>
        /// Queries for the deployment object of the specified type and component name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<T?> GetDeploymentObject<T>(V1alpha1Instance instance, string component, CancellationToken cancellationToken)
            where T : k8s.IKubernetesObject<V1ObjectMeta>
        {
            var l = await Kube.ListAsync<T>(instance.Namespace(), $"seq.k8s.datalust.co/instance={instance.Name()},seq.k8s.datalust.co/component={component}", cancellationToken);
            return l.FirstOrDefault();
        }

        /// <summary>
        /// Reconciles the secret with the deployment state.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="deployment"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="RetryException"></exception>
        async Task<V1Secret?> ReconcileDeploymentLoginSecretAsync(V1alpha1Instance instance, InstanceDeploymentSpec? deployment, CancellationToken cancellationToken)
        {
            var loginSecret = await GetDeploymentObject<V1Secret>(instance, "login-secret", cancellationToken);

            // no deployment, we have an existing admin secret owned by us, delete
            if (deployment is null && loginSecret is not null && loginSecret.IsOwnedBy(instance))
            {
                await Kube.DeleteAsync(loginSecret, cancellationToken);
                loginSecret = null;
            }

            // we have deployment
            if (deployment is not null)
            {
                var loginSecretName = deployment.LoginSecretRef?.Name ?? instance.Name() + "-login";
                var loginSecretNamespace = deployment.LoginSecretRef?.NamespaceProperty ?? instance.Namespace();
                if (loginSecretNamespace != instance.Namespace())
                    throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} could not deploy: LoginSecret must be in same namespace as Instance.");

                // we have an existing secret, owned by us
                if (loginSecret is not null && loginSecret.IsOwnedBy(instance))
                {
                    // existing secret does not match our specification
                    if (loginSecret.Name() != loginSecretName || loginSecret.Namespace() != loginSecretNamespace)
                    {
                        await Kube.DeleteAsync(loginSecret, cancellationToken);
                        loginSecret = null;
                    }
                }

                // no secret remaining, but we need one
                if (loginSecret is null)
                {
                    Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} deployment required Secret {LoginSecretName} which does not exist: creating.", EntityTypeName, instance.Namespace(), instance.Name(), loginSecretName);
                    loginSecret = await Kube.CreateAsync(
                        ApplyDeployment(
                            instance,
                            deployment,
                            "login-secret",
                            ApplyDeploymentLoginSecret(
                                instance,
                                deployment,
                                new V1Secret(metadata: new V1ObjectMeta(namespaceProperty: loginSecretNamespace, name: loginSecretName)).WithOwnerReference(instance))),
                        cancellationToken);
                }

                // we have a secret at this point, and it is owned by us, we can ensure the login information is set to defaults
                if (loginSecret.IsOwnedBy(instance))
                {
                    ApplyDeploymentLoginSecret(instance, deployment, loginSecret);
                    ApplyDeployment(instance, deployment, "login-secret", loginSecret);

                    loginSecret = await Kube.UpdateAsync(loginSecret, cancellationToken);
                }
            }

            return loginSecret;
        }

        /// <summary>
        /// Applies the deployment information to the login secret.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="deployment"></param>
        /// <param name="adminSecret"></param>
        V1Secret ApplyDeploymentLoginSecret(V1alpha1Instance instance, InstanceDeploymentSpec deployment, V1Secret adminSecret)
        {
            adminSecret.Data ??= new Dictionary<string, byte[]>();
            adminSecret.StringData ??= new Dictionary<string, string>();

            // default username
            if (adminSecret.Data.ContainsKey("username") == false)
                adminSecret.StringData["username"] = "admin";

            // default password
            if (adminSecret.Data.ContainsKey("password") == false)
                adminSecret.StringData["password"] = GeneratePassword(20);

            // default firstRun password
            if (adminSecret.Data.ContainsKey("firstRun") == false)
                adminSecret.StringData["firstRun"] = GeneratePassword(20);

            return adminSecret;
        }

        /// <summary>
        /// Reconciles the token with the deployment state.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="deployment"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<V1alpha1ApiKey?> ReconcileDeploymentAdminApiKeyAsync(V1alpha1Instance instance, InstanceDeploymentSpec deployment, CancellationToken cancellationToken)
        {
            var adminApiKey = await GetDeploymentObject<V1alpha1ApiKey>(instance, "admin-apikey", cancellationToken);

            // no deployment, we have an existing admin apikey owned by us, delete
            if (deployment is null && adminApiKey is not null && adminApiKey.IsOwnedBy(instance))
            {
                await Kube.DeleteAsync(adminApiKey, cancellationToken);
                adminApiKey = null;
            }

            // we have deployment
            if (deployment is not null)
            {
                var adminApiKeyName = instance.Name();
                var adminApiKeyNamespace = deployment.TokenSecretRef?.NamespaceProperty ?? instance.Namespace();
                if (adminApiKeyNamespace != instance.Namespace())
                    throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} could not deploy: ApiKey must be in same namespace as Instance.");

                // we have an existing secret, owned by us
                if (adminApiKey is not null && adminApiKey.IsOwnedBy(instance))
                {
                    // existing apikey does not match our specification
                    if (adminApiKey.Name() != adminApiKeyName || adminApiKey.Namespace() != adminApiKeyNamespace)
                    {
                        await Kube.DeleteAsync(adminApiKey, cancellationToken);
                        adminApiKey = null;
                    }
                }

                // no apikey remaining, but we need one
                if (adminApiKey is null)
                {
                    Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} deployment required Secret {ApiKeyName} which does not exist: creating.", EntityTypeName, instance.Namespace(), instance.Name(), adminApiKeyName);
                    adminApiKey = await Kube.CreateAsync(
                        ApplyDeployment(
                            instance,
                            deployment,
                            "admin-apikey",
                            ApplyDeploymentAdminApiKey(
                                instance,
                                deployment,
                                new V1alpha1ApiKey() { Metadata = new V1ObjectMeta(namespaceProperty: adminApiKeyNamespace, name: adminApiKeyName) }.WithOwnerReference(instance))),
                        cancellationToken);
                }

                // we have a apikey at this point, and it is owned by us, we can ensure the login information is set to defaults
                if (adminApiKey.IsOwnedBy(instance))
                {
                    ApplyDeploymentAdminApiKey(instance, deployment, adminApiKey);
                    ApplyDeployment(instance, deployment, "admin-apikey", adminApiKey);

                    adminApiKey = await Kube.UpdateAsync(adminApiKey, cancellationToken);
                }
            }

            return adminApiKey;
        }

        /// <summary>
        /// Applies the given deployment information to the specified apikey.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="deployment"></param>
        /// <param name="apikey"></param>
        /// <returns></returns>
        V1alpha1ApiKey ApplyDeploymentAdminApiKey(V1alpha1Instance instance, InstanceDeploymentSpec deployment, V1alpha1ApiKey apikey)
        {
            apikey.Spec ??= new V1alpha1ApiKeySpec();
            apikey.Spec.InstanceRef = new V1alpha1InstanceReference() { Name = instance.Name(), Namespace = instance.Namespace() };
            apikey.Spec.SecretRef = deployment.TokenSecretRef;
            apikey.Spec.Conf = new ApiKeyConf();
            apikey.Spec.Conf.Permissions = [ApiKeyPermission.Public, ApiKeyPermission.Ingest, ApiKeyPermission.Read, ApiKeyPermission.Write, ApiKeyPermission.Project, ApiKeyPermission.System, ApiKeyPermission.Organization];
            return apikey;
        }

        /// <summary>
        /// Reconciles the service account with the deployment state.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="deployment"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<V1ServiceAccount?> ReconcileDeploymentServiceAccountAsync(V1alpha1Instance instance, InstanceDeploymentSpec? deployment, CancellationToken cancellationToken)
        {
            var serviceAccount = await GetDeploymentObject<V1ServiceAccount>(instance, "service-account", cancellationToken);

            // no deployment, we have an existing service account owned by us, delete
            if (deployment is null && serviceAccount is not null && serviceAccount.IsOwnedBy(instance))
            {
                await Kube.DeleteAsync(serviceAccount, cancellationToken);
                serviceAccount = null;
            }

            // we have deployment
            if (deployment is not null)
            {
                var serviceAccountName = deployment.ServiceAccountName ?? instance.Name() + "-server";
                var serviceAccountNamespace = instance.Namespace();
                if (serviceAccountNamespace != instance.Namespace())
                    throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} could not deploy: ServiceAccount must be in same namespace as Instance.");

                // we have an existing service account, owned by us
                if (serviceAccount is not null && serviceAccount.IsOwnedBy(instance))
                {
                    // existing service account does not match our specification
                    if (serviceAccount.Name() != serviceAccountName || serviceAccount.Namespace() != serviceAccountNamespace)
                    {
                        await Kube.DeleteAsync(serviceAccount, cancellationToken);
                        serviceAccount = null;
                    }
                }

                // no secret remaining, but we need one
                if (serviceAccount is null)
                {
                    Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} deployment required ServiceAccount {ServiceAccountName} which does not exist: creating.", EntityTypeName, instance.Namespace(), instance.Name(), serviceAccountName);
                    serviceAccount = await Kube.CreateAsync(
                        ApplyDeployment(
                            instance,
                            deployment,
                            "service-account",
                            new V1ServiceAccount(metadata: new V1ObjectMeta(namespaceProperty: serviceAccountNamespace, name: serviceAccountName)).WithOwnerReference(instance)),
                        cancellationToken);
                }

                // we have a secret at this point, and it is owned by us, we can ensure the login information is set to defaults
                if (serviceAccount.IsOwnedBy(instance))
                {
                    ApplyDeployment(instance, deployment, "service-account", serviceAccount);
                    serviceAccount = await Kube.UpdateAsync(serviceAccount, cancellationToken);
                }
            }

            return serviceAccount;
        }

        /// <summary>
        /// Reconciles the stateful set with the deployment state.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="deployment"></param>
        /// <param name="adminSecret"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<V1StatefulSet?> ReconcileDeploymentStatefulSetAsync(V1alpha1Instance instance, InstanceDeploymentSpec? deployment, V1Secret? adminSecret, V1ServiceAccount? serviceAccount, V1Service? service, CancellationToken cancellationToken)
        {
            var statefulSet = await GetDeploymentObject<V1StatefulSet>(instance, "stateful-set", cancellationToken);

            // no deployment, we have an existing stateful set owned by us, delete
            if (deployment is null && statefulSet is not null && statefulSet.IsOwnedBy(instance))
            {
                await Kube.DeleteAsync(statefulSet, cancellationToken);
                statefulSet = null;
            }

            // we have deployment
            if (deployment is not null)
            {
                if (adminSecret is null)
                    throw new InvalidOperationException("AdminSecret missing.");
                if (serviceAccount is null)
                    throw new InvalidOperationException("ServiceAccount missing.");
                if (service is null)
                    throw new InvalidOperationException("Service missing.");

                var statefulSetName = instance.Name() + "-server";
                var statefulSetNamespace = instance.Namespace();
                if (statefulSetNamespace != instance.Namespace())
                    throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} could not deploy: StatefulSet must be in same namespace as Instance.");

                // we have an existing stateful set, owned by us
                if (statefulSet is not null && statefulSet.IsOwnedBy(instance))
                {
                    // existing stateful set does not match our specification
                    if (statefulSet.Name() != statefulSetName || statefulSet.Namespace() != statefulSetNamespace)
                    {
                        await Kube.DeleteAsync(statefulSet, cancellationToken);
                        statefulSet = null;
                    }
                }

                // no stateful set remaining, but we need one
                if (statefulSet is null)
                {
                    Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} deployment required StatefulSet {StatefulSetName} which does not exist: creating.", EntityTypeName, instance.Namespace(), instance.Name(), statefulSetName);
                    statefulSet = await Kube.CreateAsync(
                        ApplyDeployment(
                            instance,
                            deployment,
                            "stateful-set",
                            ApplyDeploymentStatefulSet(
                                instance,
                                deployment,
                                new V1StatefulSet(metadata: new V1ObjectMeta(namespaceProperty: instance.Namespace(), name: statefulSetName)).WithOwnerReference(instance),
                                adminSecret,
                                serviceAccount,
                                service)),
                        cancellationToken);
                }

                // we have a stateful set at this point, and it must be owned by us
                if (statefulSet.IsOwnedBy(instance) == false)
                    throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} could not deploy: StatefulSet conflict. Set is not owned by instance.");

                // update object if required
                ApplyDeploymentStatefulSet(instance, deployment, statefulSet, adminSecret, serviceAccount, service);
                ApplyDeployment(instance, deployment, "stateful-set", statefulSet);
                statefulSet = await Kube.UpdateAsync(statefulSet, cancellationToken);
            }

            return statefulSet;
        }

        /// <summary>
        /// Applies the given deployment information to the specified StatefulSet.
        /// </summary>
        /// <param name="statefulSet"></param>
        /// <param name="deployment"></param>
        /// <param name="adminSecret"></param>
        /// <param name="instance"></param>
        /// <param name="service"></param>
        V1StatefulSet ApplyDeploymentStatefulSet(V1alpha1Instance instance, InstanceDeploymentSpec deployment, V1StatefulSet statefulSet, V1Secret adminSecret, V1ServiceAccount serviceAccount, V1Service service)
        {
            statefulSet.Spec ??= new();
            statefulSet.Spec.ServiceName = serviceAccount.Name();
            statefulSet.Spec.PodManagementPolicy = "OrderedReady";
            statefulSet.Spec.Replicas = 1;
            statefulSet.Spec.Selector ??= new V1LabelSelector();
            statefulSet.Spec.UpdateStrategy ??= new V1StatefulSetUpdateStrategy();
            statefulSet.Spec.UpdateStrategy.Type = "RollingUpdate";
            statefulSet.Spec.UpdateStrategy.RollingUpdate ??= new V1RollingUpdateStatefulSetStrategy();
            statefulSet.Spec.UpdateStrategy.RollingUpdate.MaxUnavailable = 1;

            statefulSet.Spec.VolumeClaimTemplates ??= new List<V1PersistentVolumeClaim>();
            statefulSet.Spec.VolumeClaimTemplates.Clear();
            statefulSet.Spec.VolumeClaimTemplates.Add(
                new V1PersistentVolumeClaim(
                    metadata: new V1ObjectMeta(name: "seq-data"),
                    spec: new V1PersistentVolumeClaimSpec(
                        accessModes: ["ReadWriteOnce"],
                        volumeMode: "Filesystem",
                        storageClassName: deployment.Persistence?.StorageClassName,
                        resources: new V1VolumeResourceRequirements(requests: new Dictionary<string, ResourceQuantity>() { ["storage"] = new ResourceQuantity("8Gi") }))));

            // get and update template object
            var template = statefulSet.Spec.Template ??= new V1PodTemplateSpec();
            ApplyDeployment(instance, deployment, "pod", template);

            // apply match labels from template
            statefulSet.Spec.Selector.MatchLabels ??= new Dictionary<string, string>();
            statefulSet.Spec.Selector.MatchLabels.Clear();
            foreach (var kvp in template.Labels())
                statefulSet.Spec.Selector.MatchLabels[kvp.Key] = kvp.Value;

            template.Spec ??= new V1PodSpec();
            template.Spec.ServiceAccountName = serviceAccount.Name();
            template.Spec.ImagePullSecrets = deployment.ImagePullSecrets;
            template.Spec.NodeSelector = deployment.NodeSelector;
            template.Spec.Affinity = deployment.Affinity;
            template.Spec.Tolerations = deployment.Tolerations;
            template.Spec.TopologySpreadConstraints = deployment.TopologySpreadConstraints;
            template.Spec.RestartPolicy = deployment.RestartPolicy ?? "Always";
            template.Spec.TerminationGracePeriodSeconds = deployment.TerminationGracePeriodSeconds;

            template.Spec.Containers ??= new List<V1Container>();
            var container = template.Spec.Containers.FirstOrDefault(i => i.Name == "seq");
            if (container is null)
                template.Spec.Containers.Add(container = new V1Container("seq"));

            container.Image = deployment.Image ?? Options.DefaultImage;
            container.ImagePullPolicy = deployment.ImagePullPolicy ?? "IfNotPresent";

            container.Env ??= new List<V1EnvVar>();
            container.Env.Clear();
            container.Env.Add(new V1EnvVar("ACCEPT_EULA", "Y"));
            container.Env.Add(new V1EnvVar("SEQ_API_CANONICALURI", $"http://{service.Name()}.{service.Namespace()}.svc.cluster.local:80/"));
            container.Env.Add(new V1EnvVar("SEQ_API_LISTENURIS", "http://localhost:80,http://localhost:5341"));
            container.Env.Add(new V1EnvVar("SEQ_FIRSTRUN_ADMINUSERNAME", valueFrom: new V1EnvVarSource(secretKeyRef: new V1SecretKeySelector("username", adminSecret.Name(), false))));

            adminSecret.StringData ??= new Dictionary<string, string>();
            adminSecret.Data ??= new Dictionary<string, byte[]>();
            if (adminSecret.StringData.TryGetValue("firstRun", out var firstRun))
                container.Env.Add(new V1EnvVar("SEQ_FIRSTRUN_ADMINPASSWORDHASH", CalculatePasswordHash(instance, firstRun)));
            else if (adminSecret.Data.TryGetValue("firstRun", out var firstRunBuf))
                container.Env.Add(new V1EnvVar("SEQ_FIRSTRUN_ADMINPASSWORDHASH", CalculatePasswordHash(instance, Encoding.UTF8.GetString(firstRunBuf))));

            if (deployment.Env is { Count: > 0 } env)
                foreach (var i in env)
                    container.Env.Add(i);

            if (deployment.EnvFrom is { Count: > 0 } envFrom)
                foreach (var i in envFrom)
                    container.EnvFrom.Add(i);

            container.Ports ??= new List<V1ContainerPort>();
            container.Ports.Clear();
            container.Ports.Add(new V1ContainerPort(5341, name: "ingestion", protocol: "TCP"));
            container.Ports.Add(new V1ContainerPort(80, name: "ui", protocol: "TCP"));

            container.SecurityContext = new V1SecurityContext(runAsUser: 0, capabilities: new V1Capabilities(add: ["NET_BIND_SERVICE"]));

            container.VolumeMounts ??= new List<V1VolumeMount>(0);
            container.VolumeMounts.Clear();
            container.VolumeMounts.Add(new V1VolumeMount("/data", "seq-data"));

            container.LivenessProbe = new V1Probe(
                httpGet: new V1HTTPGetAction("ui", path: "/health"),
                failureThreshold: 3,
                initialDelaySeconds: 0,
                periodSeconds: 10,
                successThreshold: 1,
                timeoutSeconds: 1);

            container.ReadinessProbe = new V1Probe(
                httpGet: new V1HTTPGetAction("ui", path: "/health"),
                failureThreshold: 3,
                initialDelaySeconds: 0,
                periodSeconds: 10,
                successThreshold: 1,
                timeoutSeconds: 1);

            container.StartupProbe = new V1Probe(
                httpGet: new V1HTTPGetAction("ui", path: "/health"),
                failureThreshold: 30,
                periodSeconds: 10);

            container.Resources ??= new V1ResourceRequirements();

            if (deployment.Resources is { Claims: { } claims })
                container.Resources.Claims = claims;

            if (deployment.Resources is { Limits: { } limits })
                container.Resources.Limits = limits;

            if (deployment.Resources is { Requests: { } requests })
                container.Resources.Requests = requests;

            return statefulSet;
        }

        /// <summary>
        /// Reconciles the stateful set with the deployment state.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="deployment"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<V1Service?> ReconcileDeploymentServiceAsync(V1alpha1Instance instance, InstanceDeploymentSpec? deployment, CancellationToken cancellationToken)
        {
            var service = await GetDeploymentObject<V1Service>(instance, "service", cancellationToken);

            // no deployment, we have an existing service owned by us, delete
            if (deployment is null && service is not null && service.IsOwnedBy(instance))
            {
                await Kube.DeleteAsync(service, cancellationToken);
                service = null;
            }

            // we have deployment
            if (deployment is not null)
            {
                var serviceName = instance.Name() + "-server";
                var serviceNamespace = instance.Namespace();
                if (serviceNamespace != instance.Namespace())
                    throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} could not deploy: Service must be in same namespace as Instance.");

                // we have an existing service, owned by us
                if (service is not null && service.IsOwnedBy(instance))
                {
                    // existing service does not match our specification
                    if (service.Name() != serviceName || service.Namespace() != serviceNamespace)
                    {
                        await Kube.DeleteAsync(service, cancellationToken);
                        service = null;
                    }
                }

                // no service remaining, but we need one
                if (service is null)
                {
                    Logger.LogInformation("{EntityTypeName} {EntityNamespace}/{EntityName} deployment required Service {ServiceName} which does not exist: creating.", EntityTypeName, instance.Namespace(), instance.Name(), serviceName);
                    service = await Kube.CreateAsync(
                        ApplyDeployment(
                            instance,
                            deployment,
                            "service",
                            ApplyDeploymentService(
                                instance,
                                deployment,
                                new V1Service(metadata: new V1ObjectMeta(namespaceProperty: instance.Namespace(), name: serviceName)).WithOwnerReference(instance))),
                        cancellationToken);
                }

                // we have a service at this point, and it must be owned by us
                if (service.IsOwnedBy(instance) == false)
                    throw new RetryException($"Instance {instance.Namespace()}/{instance.Name()} could not deploy: Service conflict. Set is not owned by instance.");

                // update object if required
                ApplyDeploymentService(instance, deployment, service);
                ApplyDeployment(instance, deployment, "service", service);
                service = await Kube.UpdateAsync(service, cancellationToken);
            }

            return service;
        }

        /// <summary>
        /// Applies the given deployment information to the specified StatefulSet.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="deployment"></param>
        V1Service ApplyDeploymentService(V1alpha1Instance instance, InstanceDeploymentSpec deployment, V1Service service)
        {
            var m = service.EnsureMetadata();
            var l = m.EnsureLabels();
            var a = m.EnsureAnnotations();

            if (deployment.Service is { Labels: { } labels })
                foreach (var kvp in labels)
                    l[kvp.Key] = kvp.Value;

            if (deployment.Service is { Annotations: { } annotations })
                foreach (var kvp in annotations)
                    a[kvp.Key] = kvp.Value;

            // reset selector
            service.Spec ??= new V1ServiceSpec();
            service.Spec.Selector ??= new Dictionary<string, string>();
            service.Spec.Selector.Clear();
            service.Spec.Selector["seq.k8s.datalust.co/instance"] = instance.Name();
            service.Spec.Selector["seq.k8s.datalust.co/component"] = "pod";

            var port = new V1ServicePort(deployment.Service?.Port ?? 80, name: "http");
            if (deployment.Service?.NodePort is int nodePort)
                port.NodePort = nodePort;

            // reset ports
            service.Spec.Ports ??= new List<V1ServicePort>();
            service.Spec.Ports.Clear();
            service.Spec.Ports.Add(port);

            // further settings
            service.Spec.Type = deployment.Service?.Type ?? "ClusterIP";
            service.Spec.ClusterIP = deployment.Service?.ClusterIP ?? "";
            service.Spec.ClusterIPs = deployment.Service?.ClusterIPs;
            service.Spec.ExternalIPs = deployment.Service?.ExternalIPs;
            service.Spec.ExternalName = deployment.Service?.ExternalName;
            service.Spec.ExternalTrafficPolicy = deployment.Service?.ExternalTrafficPolicy;
            service.Spec.InternalTrafficPolicy = deployment.Service?.InternalTrafficPolicy;
            service.Spec.IpFamilies = deployment.Service?.IpFamilies;
            service.Spec.IpFamilyPolicy = deployment.Service?.IpFamilyPolicy;
            service.Spec.LoadBalancerClass = deployment.Service?.LoadBalancerClass;
            service.Spec.LoadBalancerIP = deployment.Service?.LoadBalancerIP;
            service.Spec.LoadBalancerSourceRanges = deployment.Service?.LoadBalancerSourceRanges;
            service.Spec.PublishNotReadyAddresses = deployment.Service?.PublishNotReadyAddresses;
            service.Spec.SessionAffinity = deployment.Service?.SessionAffinity;
            service.Spec.SessionAffinityConfig = deployment.Service?.SessionAffinityConfig;
            service.Spec.TrafficDistribution = deployment.Service?.TrafficDistribution;

            return service;
        }

        /// <summary>
        /// Applies the standard labels to the given entity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="component"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        T ApplyDeployment<T>(V1alpha1Instance instance, InstanceDeploymentSpec deployment, string component, T entity)
            where T : IMetadata<V1ObjectMeta>
        {
            var m = entity.EnsureMetadata();
            var l = m.EnsureLabels();
            var a = m.EnsureAnnotations();

            // apply instance labels
            if (instance.Labels() is { } labels)
                foreach (var kvp in labels)
                    l[kvp.Key] = kvp.Value;

            // apply instance annotations
            if (instance.Annotations() is { } annotations)
                foreach (var kvp in annotations)
                    a[kvp.Key] = kvp.Value;

            // apply deployment labels
            if (deployment.Labels is { } deploymentLabels)
                foreach (var kvp in deploymentLabels)
                    l[kvp.Key] = kvp.Value;

            // apply deployment annotations
            if (deployment.Annotations is { } deploymentAnnotations)
                foreach (var kvp in deploymentAnnotations)
                    a[kvp.Key] = kvp.Value;

            // apply component labels
            l["seq.k8s.datalust.co/instance"] = instance.Name();
            l["seq.k8s.datalust.co/component"] = component;
            l["app.kubernetes.io/part-of"] = "seq-server";

            return entity;
        }

        /// <inheritdoc />
        public override Task DeletedAsync(V1alpha1Instance entity, CancellationToken cancellationToken)
        {
            Logger.LogWarning("Unsupported operation deleting entity {Entity}.", entity);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Queries for the specified setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="api"></param>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<T?> GetSettingValueAsync<T>(SeqConnection api, SettingName name, CancellationToken cancellationToken)
        {
            var setting = await api.Settings.FindNamedAsync(name, cancellationToken);
            if (setting is null)
                throw new InvalidOperationException($"Unknown setting.");

            return (T?)setting.Value ?? default;
        }

        /// <summary>
        /// Gets the various info for the entity from the API.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        async Task<InstanceInfo> GetInfoAsync(SeqConnection api, CancellationToken cancellationToken)
        {
            var info = new InstanceInfo();
            await GetAuthSettingsAsync(api, info.Auth = new InstanceConfAuthenticationSpec(), cancellationToken);
            info.DataAgeWarningThresholdMilliseconds = await GetSettingValueAsync<long>(api, SettingName.DataAgeWarningThresholdMilliseconds, cancellationToken);
            info.BackupLocation = await GetSettingValueAsync<string>(api, SettingName.BackupLocation, cancellationToken);
            info.BackupsToKeep = await GetSettingValueAsync<long>(api, SettingName.BackupsToKeep, cancellationToken);
            info.BackupUtcTimeOfDay = await GetSettingValueAsync<string>(api, SettingName.BackupUtcTimeOfDay, cancellationToken);
            info.CheckForPackageUpdates = await GetSettingValueAsync<bool>(api, SettingName.CheckForPackageUpdates, cancellationToken);
            info.CheckForUpdates = await GetSettingValueAsync<bool>(api, SettingName.CheckForUpdates, cancellationToken);
            info.InstanceTitle = await GetSettingValueAsync<string>(api, SettingName.InstanceTitle, cancellationToken);
            info.MinimumFreeStorageSpace = await GetSettingValueAsync<long>(api, SettingName.MinimumFreeStorageSpace, cancellationToken);
            info.NewUserPreferences = await GetSettingValueAsync<Dictionary<string, string>>(api, SettingName.NewUserPreferences, cancellationToken);
            info.NewUserRoleIds = (await GetSettingValueAsync<string>(api, SettingName.NewUserRoleIds, cancellationToken))?.Split(",");
            info.NewUserShowSignalIds = (await GetSettingValueAsync<string>(api, SettingName.NewUserShowSignalIds, cancellationToken))?.Split(",");
            info.NewUserShowQueryIds = (await GetSettingValueAsync<string>(api, SettingName.NewUserShowQueryIds, cancellationToken))?.Split(",");
            info.NewUserShowDashboardIds = (await GetSettingValueAsync<string>(api, SettingName.NewUserShowDashboardIds, cancellationToken))?.Split(",");
            info.RequireApiKeyForWritingEvents = await GetSettingValueAsync<bool>(api, SettingName.RequireApiKeyForWritingEvents, cancellationToken);
            info.RawEventMaximumContentLength = await GetSettingValueAsync<long>(api, SettingName.RawEventMaximumContentLength, cancellationToken);
            info.RawPayloadMaximumContentLength = await GetSettingValueAsync<long>(api, SettingName.RawPayloadMaximumContentLength, cancellationToken);
            info.TargetReplicaCount = await GetSettingValueAsync<long>(api, SettingName.TargetReplicaCount, cancellationToken);
            info.ThemeStyles = await GetSettingValueAsync<string>(api, SettingName.ThemeStyles, cancellationToken);
            return info;
        }

        /// <summary>
        /// Gets the authentication settings from the API and outputs them to the <see cref="InstanceConfSettings.AuthConf"/> object.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="info"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task GetAuthSettingsAsync(SeqConnection api, InstanceConfAuthenticationSpec info, CancellationToken cancellationToken)
        {
            if (await GetSettingValueAsync<bool?>(api, SettingName.IsAuthenticationEnabled, cancellationToken) == true)
            {
                switch (await GetSettingValueAsync<string>(api, SettingName.AuthenticationProvider, cancellationToken))
                {
                    case null:
                        info.Local = new InstanceConfAuthenticationSpec.LocalAuth();
                        break;
                    case "Active Directory":
                        info.ActiveDirectory = new InstanceConfAuthenticationSpec.ActiveDirectoryAuth();
                        info.ActiveDirectory.AutomaticAccessADGroup = await GetSettingValueAsync<string>(api, SettingName.AutomaticAccessADGroup, cancellationToken);
                        info.AutomaticallyProvisionAuthenticatedUsers = await GetSettingValueAsync<bool>(api, SettingName.AutomaticallyProvisionAuthenticatedUsers, cancellationToken);
                        break;
                    case "Microsoft Entra ID":
                        info.Entra = new InstanceConfAuthenticationSpec.EntraAuth();
                        info.Entra.Authority = await GetSettingValueAsync<string>(api, SettingName.EntraIDAuthority, cancellationToken);
                        info.Entra.TenantId = await GetSettingValueAsync<string>(api, SettingName.EntraIDTenantId, cancellationToken);
                        info.Entra.ClientId = await GetSettingValueAsync<string>(api, SettingName.EntraIDClientId, cancellationToken);
                        info.Entra.ClientKey = await GetSettingValueAsync<string>(api, SettingName.EntraIDClientKey, cancellationToken);
                        info.AutomaticallyProvisionAuthenticatedUsers = await GetSettingValueAsync<bool>(api, SettingName.AutomaticallyProvisionAuthenticatedUsers, cancellationToken);
                        return;
                    case "OpenID Connect":
                        info.Oidc = new InstanceConfAuthenticationSpec.OidcAuth();
                        info.Oidc.Authority = await GetSettingValueAsync<string>(api, SettingName.OpenIdConnectAuthority, cancellationToken);
                        info.Oidc.ClientId = await GetSettingValueAsync<string>(api, SettingName.OpenIdConnectClientId, cancellationToken);
                        info.Oidc.ClientSecret = await GetSettingValueAsync<string>(api, SettingName.OpenIdConnectClientSecret, cancellationToken);
                        info.Oidc.Scopes = await GetSettingValueAsync<string[]>(api, SettingName.OpenIdConnectScopes, cancellationToken);
                        info.Oidc.MetadataAddress = await GetSettingValueAsync<string>(api, SettingName.OpenIdConnectMetadataAddress, cancellationToken);
                        info.AutomaticallyProvisionAuthenticatedUsers = await GetSettingValueAsync<bool>(api, SettingName.AutomaticallyProvisionAuthenticatedUsers, cancellationToken);
                        break;
                }
            }
        }

        /// <summary>
        /// Queries for the specified setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="api"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task PutSettingValueAsync<T>(V1alpha1Instance instance, SeqConnection api, SettingName name, T? value, CancellationToken cancellationToken)
        {
            Logger.LogDebug("{EntityTypeName} {EntityNamespace}/{EntityName} putting Seq setting {SettingName}.", EntityTypeName, instance.Namespace(), instance.Name(), name);
            var setting = await api.Settings.FindNamedAsync(name, cancellationToken);
            setting.Value = value;
            await api.Settings.UpdateAsync(setting, cancellationToken);
        }

        /// <summary>
        /// Puts the configuration to the API.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="api"></param>
        /// <param name="info"></param>
        /// <param name="conf"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task PutConfAsync(V1alpha1Instance instance, SeqConnection api, InstanceInfo? info, InstanceConf conf, CancellationToken cancellationToken)
        {
            if (conf is not null)
            {
                if (conf.Auth is not null)
                    await PutAuthSettingsAsync(instance, api, info?.Auth, conf.Auth, cancellationToken);

                if (conf.DataAgeWarningThresholdMilliseconds is long dataAgeWarningThresholdMilliseconds)
                    if (info == null || info.DataAgeWarningThresholdMilliseconds != dataAgeWarningThresholdMilliseconds)
                        await PutSettingValueAsync(instance, api, SettingName.DataAgeWarningThresholdMilliseconds, dataAgeWarningThresholdMilliseconds, cancellationToken);

                if (conf.BackupLocation is string backupLocation)
                    if (info == null || info.BackupLocation != backupLocation)
                        await PutSettingValueAsync(instance, api, SettingName.BackupLocation, backupLocation, cancellationToken);

                if (conf.BackupsToKeep is long backupsToKeep)
                    if (info == null || info.BackupsToKeep != backupsToKeep)
                        await PutSettingValueAsync(instance, api, SettingName.BackupsToKeep, backupsToKeep, cancellationToken);

                if (conf.BackupUtcTimeOfDay is string backupUtcTimeOfDay)
                    if (info == null || info.BackupUtcTimeOfDay != backupUtcTimeOfDay)
                        await PutSettingValueAsync(instance, api, SettingName.BackupUtcTimeOfDay, backupUtcTimeOfDay, cancellationToken);

                if (conf.CheckForPackageUpdates is bool checkForPackageUpdates)
                    if (info == null || info.CheckForPackageUpdates != checkForPackageUpdates)
                        await PutSettingValueAsync(instance, api, SettingName.CheckForPackageUpdates, checkForPackageUpdates, cancellationToken);

                if (conf.CheckForUpdates is bool checkForUpdates)
                    if (info == null || info.CheckForUpdates != checkForUpdates)
                        await PutSettingValueAsync(instance, api, SettingName.CheckForUpdates, checkForUpdates, cancellationToken);

                if (conf.InstanceTitle is string instanceTitle)
                    if (info == null || info.InstanceTitle != instanceTitle)
                        await PutSettingValueAsync(instance, api, SettingName.InstanceTitle, instanceTitle, cancellationToken);

                if (conf.MinimumFreeStorageSpace is long minimumFreeStorageSpace)
                    if (info == null || info.MinimumFreeStorageSpace != minimumFreeStorageSpace)
                        await PutSettingValueAsync(instance, api, SettingName.MinimumFreeStorageSpace, minimumFreeStorageSpace, cancellationToken);

                //if (conf.NewUserPreferences is long newUserPreferences)
                //    if (info == null ||  info.NewUserPreferences != newUserPreferences)
                //        await PutSettingValueAsync(instance, api, SettingName.NewUserPreferences, newUserPreferences, cancellationToken);

                //if (conf.NewUserRoleIds is long newUserRoleIds)
                //    if (info == null ||  info.NewUserRoleIds != newUserRoleIds)
                //        await PutSettingValueAsync(instance, api, SettingName.NewUserRoleIds, newUserRoleIds, cancellationToken);

                //if (conf.NewUserShowSignalIds is long newUserShowSignalIds)
                //    if (info == null ||  info.NewUserShowSignalIds != newUserShowSignalIds)
                //        await PutSettingValueAsync(instance, api, SettingName.NewUserShowSignalIds, newUserShowSignalIds, cancellationToken);

                //if (conf.NewUserShowQueryIds is long newUserShowQueryIds)
                //    if (info == null ||  info.NewUserShowQueryIds != newUserShowQueryIds)
                //        await PutSettingValueAsync(instance, api, SettingName.NewUserShowQueryIds, newUserShowQueryIds, cancellationToken);

                //if (conf.NewUserShowDashboardIds is long newUserShowDashboardIds)
                //    if (info == null ||  info.NewUserShowDashboardIds != newUserShowDashboardIds)
                //        await PutSettingValueAsync(instance, api, SettingName.NewUserShowDashboardIds, newUserShowDashboardIds, cancellationToken);

                if (conf.RequireApiKeyForWritingEvents is bool requireApiKeyForWritingEvents)
                    if (info == null || info.RequireApiKeyForWritingEvents != requireApiKeyForWritingEvents)
                        await PutSettingValueAsync(instance, api, SettingName.RequireApiKeyForWritingEvents, requireApiKeyForWritingEvents, cancellationToken);

                if (conf.RawEventMaximumContentLength is long rawEventMaximumContentLength)
                    if (info == null || info.RawEventMaximumContentLength != rawEventMaximumContentLength)
                        await PutSettingValueAsync(instance, api, SettingName.RawEventMaximumContentLength, rawEventMaximumContentLength, cancellationToken);

                if (conf.RawPayloadMaximumContentLength is long rawPayloadMaximumContentLength)
                    if (info == null || info.RawPayloadMaximumContentLength != rawPayloadMaximumContentLength)
                        await PutSettingValueAsync(instance, api, SettingName.RawPayloadMaximumContentLength, rawPayloadMaximumContentLength, cancellationToken);

                if (conf.TargetReplicaCount is long targetReplicaCount)
                    if (info == null || info.TargetReplicaCount != targetReplicaCount)
                        await PutSettingValueAsync(instance, api, SettingName.TargetReplicaCount, targetReplicaCount, cancellationToken);

                if (conf.ThemeStyles is string themeStyles)
                    if (info == null || info.ThemeStyles != themeStyles)
                        await PutSettingValueAsync(instance, api, SettingName.ThemeStyles, themeStyles, cancellationToken);
            }
        }

        /// <summary>
        /// Puts the given authentication configuration to the API.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="info"></param>
        /// <param name="conf"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task PutAuthSettingsAsync(V1alpha1Instance instance, SeqConnection api, InstanceConfAuthenticationSpec? info, InstanceConfAuthenticationSpec conf, CancellationToken cancellationToken)
        {
            if (conf.Local is { } local)
            {
                // only apply if existing authentication mode is not local
                if (info is null or { Local: null })
                {
                    await PutSettingValueAsync(instance, api, SettingName.IsAuthenticationEnabled, true, cancellationToken);
                    await PutSettingValueAsync<string>(instance, api, SettingName.AuthenticationProvider, null, cancellationToken);
                }

                return;
            }

            if (conf.ActiveDirectory is { } activeDirectory)
            {
                if (info is null or { ActiveDirectory: null })
                {
                    await PutSettingValueAsync(instance, api, SettingName.IsAuthenticationEnabled, true, cancellationToken);
                    await PutSettingValueAsync(instance, api, SettingName.AuthenticationProvider, "Active Directory", cancellationToken);
                }

                if (activeDirectory.AutomaticAccessADGroup is not null)
                    if (info?.ActiveDirectory?.AutomaticAccessADGroup != activeDirectory.AutomaticAccessADGroup)
                        await PutSettingValueAsync(instance, api, SettingName.AutomaticAccessADGroup, activeDirectory.AutomaticAccessADGroup, cancellationToken);

                if (conf.AutomaticallyProvisionAuthenticatedUsers is not null)
                    if (info?.AutomaticallyProvisionAuthenticatedUsers != conf.AutomaticallyProvisionAuthenticatedUsers)
                        await PutSettingValueAsync(instance, api, SettingName.AutomaticallyProvisionAuthenticatedUsers, conf.AutomaticallyProvisionAuthenticatedUsers, cancellationToken);

                return;
            }

            if (conf.Entra is { } entra)
            {
                if (info is null or { Entra: null })
                {
                    await PutSettingValueAsync(instance, api, SettingName.IsAuthenticationEnabled, true, cancellationToken);
                    await PutSettingValueAsync(instance, api, SettingName.AuthenticationProvider, "Microsoft Entra ID", cancellationToken);
                }

                if (entra.Authority is not null)
                    if (info?.Entra?.Authority != entra.Authority)
                        await PutSettingValueAsync(instance, api, SettingName.EntraIDAuthority, entra.Authority, cancellationToken);

                if (entra.TenantId is not null)
                    if (info?.Entra?.TenantId != entra.TenantId)
                        await PutSettingValueAsync(instance, api, SettingName.EntraIDTenantId, entra.TenantId, cancellationToken);

                if (entra.ClientId is not null)
                    if (info?.Entra?.ClientId != entra.ClientId)
                        await PutSettingValueAsync(instance, api, SettingName.EntraIDClientId, entra.ClientId, cancellationToken);

                if (entra.ClientKey is not null)
                    if (info?.Entra?.ClientKey != entra.ClientKey)
                        await PutSettingValueAsync(instance, api, SettingName.EntraIDClientKey, entra.ClientKey, cancellationToken);

                if (conf.AutomaticallyProvisionAuthenticatedUsers is not null)
                    if (info?.AutomaticallyProvisionAuthenticatedUsers != conf.AutomaticallyProvisionAuthenticatedUsers)
                        await PutSettingValueAsync(instance, api, SettingName.AutomaticallyProvisionAuthenticatedUsers, conf.AutomaticallyProvisionAuthenticatedUsers, cancellationToken);

                return;
            }

            if (conf.Oidc is { } oidc)
            {
                if (info is null or { Oidc: null })
                {
                    await PutSettingValueAsync(instance, api, SettingName.IsAuthenticationEnabled, true, cancellationToken);
                    await PutSettingValueAsync(instance, api, SettingName.AuthenticationProvider, "OpenID Connect", cancellationToken);
                }

                if (oidc.Authority is not null)
                    if (info?.Oidc?.Authority != oidc.Authority)
                        await PutSettingValueAsync(instance, api, SettingName.OpenIdConnectAuthority, oidc.Authority, cancellationToken);

                if (oidc.ClientId is not null)
                    if (info?.Oidc?.ClientId != oidc.ClientId)
                        await PutSettingValueAsync(instance, api, SettingName.OpenIdConnectClientId, oidc.ClientId, cancellationToken);

                if (oidc.ClientSecret is not null)
                    if (info?.Oidc?.ClientSecret != oidc.ClientSecret)
                        await PutSettingValueAsync(instance, api, SettingName.OpenIdConnectClientSecret, oidc.ClientSecret, cancellationToken);

                if (oidc.MetadataAddress is not null)
                    if (info?.Oidc?.MetadataAddress != oidc.MetadataAddress)
                        await PutSettingValueAsync(instance, api, SettingName.OpenIdConnectMetadataAddress, oidc.MetadataAddress, cancellationToken);

                if (oidc.Scopes is not null)
                    if (SetEqual(info?.Oidc?.Scopes, oidc.Scopes) == false)
                        await PutSettingValueAsync(instance, api, SettingName.OpenIdConnectScopes, oidc.Scopes, cancellationToken);

                if (conf.AutomaticallyProvisionAuthenticatedUsers is not null)
                    if (info?.AutomaticallyProvisionAuthenticatedUsers != conf.AutomaticallyProvisionAuthenticatedUsers)
                        await PutSettingValueAsync(instance, api, SettingName.AutomaticallyProvisionAuthenticatedUsers, conf.AutomaticallyProvisionAuthenticatedUsers, cancellationToken);

                return;
            }

            // disable authentication if previously enabled
            if (info is null or { Local: not null } or { ActiveDirectory: not null } or { Entra: not null } or { Oidc: not null })
                await PutSettingValueAsync(instance, api, SettingName.IsAuthenticationEnabled, false, cancellationToken);
        }

        /// <summary>
        /// Returns <c>true</c> if the sets are equal.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        bool SetEqual<T>(T[]? a, T[]? b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is not null && b is null)
                return false;

            if (a is null && b is not null)
                return false;

            return a.ToHashSet().SetEquals(b.ToHashSet());
        }

    }

}
