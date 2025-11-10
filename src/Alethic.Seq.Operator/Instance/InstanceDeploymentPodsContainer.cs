using System.Collections.Generic;
using System.Text.Json.Serialization;

using k8s.Models;

namespace Alethic.Seq.Operator.Instance
{

    /// <summary>
    /// Describes how to deploy a new instance of Seq.
    /// </summary>
    public class InstanceDeploymentPodsContainer
    {

        /// <summary>
        /// List of environment variables to set in the container. Cannot be updated.
        /// </summary>
        [JsonPropertyName("env")]
        public IList<V1EnvVar>? Env { get; set; }

        /// <summary>
        /// List of sources to populate environment variables in the container. The keys
        /// defined within a source must be a C_IDENTIFIER. All invalid keys will be
        /// reported as an event when the container is starting. When a key exists in
        /// multiple sources, the value associated with the last source will take
        /// precedence. Values defined by an Env with a duplicate key will take precedence.
        /// Cannot be updated.
        /// </summary>
        [JsonPropertyName("envFrom")]
        public IList<V1EnvFromSource>? EnvFrom { get; set; }

        /// <summary>
        /// Container image name. More info:
        /// https://kubernetes.io/docs/concepts/containers/images This field is optional to
        /// allow higher level config management to default or override container images in
        /// workload controllers like Deployments and StatefulSets.
        /// </summary>
        [JsonPropertyName("image")]
        public string? Image { get; set; }

        /// <summary>
        /// Image pull policy. One of Always, Never, IfNotPresent. Defaults to Always if
        /// :latest tag is specified, or IfNotPresent otherwise. Cannot be updated. More
        /// info: https://kubernetes.io/docs/concepts/containers/images#updating-images
        /// </summary>
        [JsonPropertyName("imagePullPolicy")]
        public string? ImagePullPolicy { get; set; }

    }

}
