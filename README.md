# Seq Operator for Kubernetes

A Kubernetes operator for [Seq](https://datalust.co/seq). Describe Seq servers and their configuration as
Kubernetes resources, and the operator keeps the server in step with them.

It can:

- **Deploy Seq** into the cluster as a StatefulSet with persistent storage, bootstrap its admin account and
  issue itself a management API key.
- **Manage an existing Seq server**, inside or outside the cluster, given a login or an API key.
- **Configure Seq declaratively**: server settings, authentication providers, licensing, API keys, signals and
  retention policies.
- **Hand API keys to workloads** as Kubernetes Secrets containing the Seq endpoint and token.

> [!NOTE]
> The resources are at `v1alpha1`. They work and are in use, but their shape may still change between releases.
> See [Limitations](#limitations).

## Installation

The operator is published as a Helm chart and container image on GitHub Container Registry.

```bash
helm install seq-operator oci://ghcr.io/alethic/seq-operator/seq-operator \
  --namespace seq-operator \
  --create-namespace
```

Without `--version`, Helm installs the latest stable release. Builds from `main` are published as prereleases
(for example `0.1.0-pre.145`); install one with `--version <version>`, or with `--devel` for the newest.

### Chart values

| value | default | description |
| --- | --- | --- |
| `operator.image.repository` | `ghcr.io/alethic/seq-operator/seq-operator-image` | Operator image. |
| `operator.image.tag` | chart `appVersion` | Operator image tag. `operator.image.digest` takes precedence if set. |
| `operator.resources` | 50m CPU, 64Mi requested | Operator pod resources. |
| `options.logLevel` | `Information` | Operator log level. |
| `options.defaultImage` | `datalust/seq:latest` | Seq image used by deployed Instances that don't set `deployment.image`. |
| `options.reconciliation.interval` | `00:05:00` | Time between periodic reconciliations. |
| `options.reconciliation.retryInterval` | `00:00:30` | Time before retrying a failed reconciliation. |
| `crds.enabled` | `true` | Install and upgrade the CRDs with the chart. Set `false` to manage them yourself. |
| `crds.keep` | `false` | Annotate the CRDs with `helm.sh/resource-policy: keep` so they survive `helm uninstall`. |

See [`values.yaml`](charts/seq-operator/values.yaml) for scheduling, security context and other pod settings.

## Resources

All resources are in the `seq.k8s.datalust.co/v1alpha1` API group.

| kind | short name | manages |
| --- | --- | --- |
| `Instance` | `seqinstance` | A Seq server: deployed by the operator, or an existing one it connects to. Holds server settings. |
| `ApiKey` | `seqapikey` | A Seq API key, published to a Kubernetes Secret. |
| `Signal` | `seqsignal` | A shared Seq signal. |
| `RetentionPolicy` | | A Seq retention policy. |
| `Alert` | | A Seq alert (title only; see [Limitations](#limitations)). |

Everything other than `Instance` points at an Instance with `spec.instanceRef`, and puts the settings it
manages under `spec.conf`.

### Deploying Seq

An Instance without a `remote` section is deployed by the operator. This is the smallest one:

```yaml
apiVersion: seq.k8s.datalust.co/v1alpha1
kind: Instance
metadata:
  name: seq
  namespace: logging
spec: {}
```

The operator then:

1. Creates a Secret, `seq-operator-login`, with an `admin` username, a generated password and a separate
   generated first-run password.
2. Creates a StatefulSet running Seq with a 32Gi volume, and a Service named after the Instance. Seq is reachable
   in the cluster at `http://seq.logging.svc.cluster.local`.
3. Logs in with the first-run password and changes it to the real one.
4. Creates an ApiKey, `seq-operator`, and uses that key to manage the server from then on.

The admin login is in the `seq-operator-login` Secret.

The `deployment` section adjusts the deployment. It accepts the usual pod settings: `image`, `resources`,
`env`, `affinity`, `nodeSelector`, `tolerations`, `topologySpreadConstraints`, `imagePullSecrets`, the three
probe types, `annotations` and `labels`. It also takes `persistence` and `service`:

```yaml
apiVersion: seq.k8s.datalust.co/v1alpha1
kind: Instance
metadata:
  name: seq
  namespace: logging
spec:
  deployment:
    image: datalust/seq:2025.2
    resources:
      requests:
        cpu: 500m
        memory: 2Gi
    persistence:
      storageClassName: fast-ssd
      capacity: 100Gi
    service:
      type: LoadBalancer
```

Pin `image` to a specific Seq version. The default, `datalust/seq:latest`, can upgrade Seq whenever the pod is
rescheduled.

### Connecting to an existing Seq server

Give an Instance a `remote` section to manage a server the operator didn't deploy. `remote.auth` lists the
ways to authenticate, tried in order:

- `token` reads a Secret with a `token` key holding a Seq API key.
- `login` reads a Secret with `username` and `password` keys. If the password is rejected and the Secret also
  has a `firstRun` key, the operator logs in with that instead and changes the password to `password`.

That second case bootstraps a fresh server. Start Seq with `SEQ_FIRSTRUN_ADMINPASSWORDHASH` set to the hash of
the first-run password, and let the operator create its own API key:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: seq-login
  namespace: logging
type: Opaque
stringData:
  username: admin
  password: "<the password the operator should set>"
  firstRun: "<the password Seq was first started with>"
---
apiVersion: seq.k8s.datalust.co/v1alpha1
kind: Instance
metadata:
  name: seq
  namespace: logging
spec:
  remote:
    endpoint: http://seq.example.com
    auth:
    # Prefer the API key once the ApiKey below has created it...
    - token:
        secretRef:
          name: seq-management
    # ...and fall back to the login until then.
    - login:
        secretRef:
          name: seq-login
---
apiVersion: seq.k8s.datalust.co/v1alpha1
kind: ApiKey
metadata:
  name: seq-management
  namespace: logging
spec:
  instanceRef:
    name: seq
  secretRef:
    name: seq-management
  conf:
    title: Kubernetes operator
    permissions:
    - Read
    - Write
    - Project
    - System
    - Organization
```

### Server settings

`spec.conf` on an Instance sets Seq server settings. Anything left out is not changed.

```yaml
spec:
  conf:
    instanceTitle: Production
    requireApiKeyForWritingEvents: true
    minimumFreeStorageSpace: 10737418240
    backupLocation: /data/backups
    backupsToKeep: 7
    backupUtcTimeOfDay: "02:00:00"
    license:
      secretKeyRef:
        name: seq-license
        key: license
      automaticallyRefresh: true
    auth:
      oidc:
        authority: https://login.example.com
        clientId: seq
        clientSecretRef:
          name: seq-oidc
          key: clientSecret
        scopes: [openid, profile, email]
      automaticallyProvisionAuthenticatedUsers: true
```

`auth` accepts one of `local`, `activeDirectory`, `entra` or `oidc`.

Give the OIDC client secret as `clientSecretRef`, and the Entra client key as `clientKeyRef`. Each is a key of a
Secret in the Instance's namespace. The plain-text `clientSecret` and `clientKey` fields still work but are
deprecated, since anyone who can read the Instance can read them; set one form or the other, not both. Either way,
the Instance's status shows `<set>` in place of the value.

### API keys

An ApiKey creates a Seq API key and writes it to a Secret with two keys: `endpoint`, the Seq server's URL, and
`token`, the API key. The Secret is named by `secretRef`, or `<name>-seq-apikey` if that's omitted, and must be in
the ApiKey's namespace.

```yaml
apiVersion: seq.k8s.datalust.co/v1alpha1
kind: ApiKey
metadata:
  name: orders
  namespace: orders
spec:
  instanceRef:
    name: seq
    namespace: logging
  conf:
    title: Orders service
    permissions:
    - Ingest
    inputSettings:
      minimumLevel: Information
      appliedProperties:
        Application: orders
```

A workload can then take its Seq settings straight from the Secret:

```yaml
env:
- name: SEQ_SERVER_URL
  valueFrom:
    secretKeyRef:
      name: orders-seq-apikey
      key: endpoint
- name: SEQ_API_KEY
  valueFrom:
    secretKeyRef:
      name: orders-seq-apikey
      key: token
```

### Signals and retention policies

The Instance must grant `signals` permission for a Signal to attach; see
[Which namespaces may use an Instance](#which-namespaces-may-use-an-instance).

```yaml
apiVersion: seq.k8s.datalust.co/v1alpha1
kind: Signal
metadata:
  name: errors
  namespace: logging
spec:
  instanceRef:
    name: seq
  conf:
    title: Errors
    description: Events at Error level or above
    grouping: Explicit
    explicitGroupName: Levels
    filters:
    - description: Errors
      filter: "@Level in ['Error', 'Fatal']"
    columns:
    - expression: Application
---
apiVersion: seq.k8s.datalust.co/v1alpha1
kind: RetentionPolicy
metadata:
  name: thirty-days
  namespace: logging
spec:
  instanceRef:
    name: seq
  conf:
    retentionTime: "30.00:00:00"
```

`retentionTime` is a .NET `TimeSpan`, written `d.hh:mm:ss`.

### Which namespaces may use an Instance

By default an Instance only accepts ApiKeys, RetentionPolicies and Alerts from its own namespace. Signals are not
in the defaults: an Instance accepts them only with an explicit `signals` permission. To change any of this, set
`spec.permissions`.

`spec.permissions` is a list of entries. For each resource, the first entry whose `namespaces` matches the
resource's namespace decides what it may do. `from` is one of `Same` (the default), `All`, `Selector` or `None`.
Setting `spec.permissions` replaces the defaults entirely.

Each entry takes `apiKeys`, `alerts`, `signals` and `retentionPolicies`, each with `create` (create new objects in
Seq) and `attach` (adopt existing ones). `apiKeys` also controls what a key may be:

| flag | controls | when not set |
| --- | --- | --- |
| `setTitle` | setting `conf.title` | denied |
| `setIngest` | assigning `Ingest` | **allowed** |
| `setPublic` | assigning `Public` | **allowed** |
| `setRead` | assigning `Read` | denied |
| `setWrite` | assigning `Write` | denied |
| `setProject` | assigning `Project` | denied |
| `setSystem` | assigning `System` | denied |
| `setOrganization` | assigning `Organization` | denied |

`Ingest` and `Public` only let a key send events, so they're allowed unless an entry sets the flag to `false`.
Everything that reads or changes Seq has to be granted. An ApiKey asking for a permission it isn't allowed is
refused as a whole, with the reason in its status, rather than created with fewer permissions. If the key already
exists in Seq, it's left as it was.

This lets any namespace create ingest-only keys, while namespaces labelled `seq-access: read` may also create keys
that read:

```yaml
spec:
  permissions:
  - namespaces:
      from: Same
    apiKeys:
      create: true
      setTitle: true
      setRead: true
      setWrite: true
      setProject: true
      setSystem: true
      setOrganization: true
  - namespaces:
      from: Selector
      selector:
        matchLabels:
          seq-access: read
    apiKeys:
      create: true
      setTitle: true
      setRead: true
  - namespaces:
      from: All
    apiKeys:
      create: true
      setTitle: true
```

The first entry keeps the Instance's own namespace fully privileged. Without it, listing other namespaces would
take that away.

### Deletion

Each resource carries a finalizer. Deleting an ApiKey, Signal, RetentionPolicy or Alert deletes the matching
object in Seq, provided the operator can still reach the Instance. If it can't, the resource stays in
`Terminating` until the Instance is reachable again or the finalizer is removed by hand.

## Upgrading

### ApiKey permission flags are enforced

Earlier versions accepted the `apiKeys.set*` flags but never checked them, so any namespace allowed to create
ApiKeys could assign any Seq permission. They're now enforced as described in
[Which namespaces may use an Instance](#which-namespaces-may-use-an-instance).

Before upgrading, find ApiKeys that ask for more than `Ingest` or `Public` from outside their Instance's namespace,
and grant what they need in the Instance's `spec.permissions`. Otherwise they will be refused from then on. Their
keys in Seq keep working; they just stop being updated. Instances without `spec.permissions` are unaffected.

### SSO secrets move to Secrets

Earlier versions copied the OIDC client secret and Entra client key into each Instance's status, where anyone with
the view role could read them. Status now shows `<set>` instead, and existing statuses are rewritten on the next
reconcile. To take the secret out of the spec too, move it into a Secret and replace `clientSecret` with
`clientSecretRef` (or `clientKey` with `clientKeyRef`). Remove the plain-text field when you do: setting both is
refused.

### Adopting the CustomResourceDefinitions (one time)

The chart installs its CRDs as ordinary templates, so `helm upgrade` keeps them up to date. Releases installed
before this change own their CRDs outside of Helm, and the first upgrade fails with:

```
Error: UPGRADE FAILED: rendered manifests contain a resource that already exists.
Unable to continue with update: CustomResourceDefinition "instances.seq.k8s.datalust.co" ...
invalid ownership metadata
```

Hand the existing CRDs to the release once, before upgrading:

```bash
RELEASE=seq-operator
NAMESPACE=seq-operator

for c in alerts apikeys instances retentionpolicys signals; do
  kubectl annotate crd "$c.seq.k8s.datalust.co" \
    "meta.helm.sh/release-name=$RELEASE" \
    "meta.helm.sh/release-namespace=$NAMESPACE" --overwrite
  kubectl label crd "$c.seq.k8s.datalust.co" \
    app.kubernetes.io/managed-by=Helm --overwrite
done
```

Fresh installs don't need this.

## Limitations

- **Alerts** only manage the alert's title. Conditions, notifications and the rest have to be set in Seq.
- **Signals**: filters and columns removed from the spec are not removed from Seq.
- **Persistence** (`deployment.persistence`) can't be changed once an Instance is deployed, since Kubernetes
  doesn't allow a StatefulSet's volume claims to change.
- **Namespace selectors** support `matchLabels` but not `matchExpressions`.
- **Restricting the operator to one namespace** is not supported yet; it always watches the whole cluster.
- **Secrets in other namespaces**: an Instance's `remote.auth` can name a Secret in any namespace. In a
  multi-tenant cluster, restrict who can create Instances.

## Building

Requires the .NET 10 SDK and Helm.

```bash
dotnet tool restore
dotnet msbuild Alethic.Seq.Operator.dist.msbuildproj
```

This builds the operator, generates the CRDs and writes the container image, Helm chart and tests to `dist/`.
Run the tests with:

```bash
dotnet exec dist/tests/Alethic.Seq.Operator.Tests/net10.0/Alethic.Seq.Operator.Tests.dll
```

## Releasing

Versions come from [GitVersion](https://gitversion.net), configured in [`GitVersion.yml`](GitVersion.yml); no
version is written anywhere in the source. Every push to `main` builds and publishes a prerelease, such as
`0.1.0-pre.145`, to GitHub Container Registry.

To release a stable version, create a GitHub Release on `main` with a new tag named for the version, such as
`0.1.0`. GitVersion takes the version from the tag. The workflow then attaches the image and chart to the
release and pushes both to the registry. Prereleases that follow carry the next patch version. To bump the minor
or major version instead, include `+semver: minor` or `+semver: major` in a commit message.

## License

[MIT](LICENSE)
