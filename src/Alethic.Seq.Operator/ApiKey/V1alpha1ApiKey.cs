using Alethic.Seq.Operator.ApiKey;

using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.ApiKey
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1alpha1", Kind = "ApiKey")]
    [KubernetesEntityShortNames("seqapikey")]
    public partial class V1alpha1ApiKey :
        CustomKubernetesEntity<V1alpha1ApiKeySpec, V1alpha1ApiKeyStatus>,
        V1alpha1InstanceEntity<V1alpha1ApiKeySpec, V1alpha1ApiKeyStatus, ApiKeyConf, ApiKeyInfo>
    {
        
    }

}
