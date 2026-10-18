using k8s.Models;

using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Alethic.Seq.Operator.ApiKey
{

    [EntityScope(EntityScope.Namespaced)]
    [KubernetesEntity(Group = "seq.k8s.datalust.co", ApiVersion = "v1", Kind = "ApiKey")]
    [StorageVersion]
    [KubernetesEntityShortNames("seqapikey")]
    public partial class V1ApiKey :
        CustomKubernetesEntity<V1ApiKeySpec, V1ApiKeyStatus>,
        V1InstanceEntity<V1ApiKeySpec, V1ApiKeyStatus, ApiKeyConf, ApiKeyInfo>
    {
        
    }

}
