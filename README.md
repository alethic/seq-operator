# Kubernetes operator for Seq

## About The Project

This Seq Kubernetes Operator is responsible for managing the lifecycle of Seq resources in a Kubernetes cluster.

### Installation

```bash
helm install seq-operator oci://ghcr.io/alethic/seq-operator/seq-operator \
  --namespace seq-operator \
  --create-namespace
```

## Usage

This is an example for connecting to and bootstrapping an existing Seq instance deployed with a SEQ_FIRSTRUN_ADMINPASSWORDHASH setting.

```

apiVersion: v1
kind: Secret
metadata:
  name: seqlogin
  namespace: default
type: Opaque
stringData:
  username: "admin"
  password: "newpassword"
  firstRun: "1234"
---

apiVersion: seq.k8s.datalust.co/v1alpha1
kind: Instance
metadata:
  name: seqinstance
  namespace: default
spec:
  remote:
    endpoint: http://localhost:5341
    auth:
    - token:
        secretRef:
          name: seqtoken
    - login:
        secretRef:
          name: seqlogin
---

apiVersion: seq.k8s.datalust.co/v1alpha1
kind: ApiKey
metadata:
  name: seqtoken
  namespace: default
spec:
  instanceRef:
    name: seqinstance
  secretRef:
    name: seqtoken
  conf:
    title: ManagementKey
    permissions:
    - System
    - Project
    - Write
    - Organization
    - Read
---

```

This is an example for deploying a new Seq instance. Simply don't specify the `remote` section.

```
apiVersion: seq.k8s.datalust.co/v1alpha1
kind: Instance
metadata:
  name: seqinstance
  namespace: default
spec: {}
```

This will generate a secret for the 'admin' login, with a randomlly generated first-run password and user password. It will bring up the instance. Once up, it will authenticate as the first-run password, change the password to the real-password, then generate an ApiToken which it will use for management purposes from that point forward.

## Upgrading

### Adopting the CustomResourceDefinitions (one time)

The chart now ships its CRDs as templates rather than from the chart's `crds/` directory, so that
`helm upgrade` applies schema changes. Helm installs `crds/` exactly once and never updates it, which
previously meant applying CRD changes by hand.

Releases installed before this change own their CRDs outside of Helm, and Helm refuses to take over a
resource it does not own:

```
Error: UPGRADE FAILED: rendered manifests contain a resource that already exists.
Unable to continue with update: CustomResourceDefinition "instances.seq.k8s.datalust.co" ...
invalid ownership metadata
```

Helm checks the metadata on the live object, so this cannot be fixed from inside the chart. Label and
annotate the existing CRDs once, before the first upgrade, substituting your release name and namespace:

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

Fresh installations need none of this.

Two chart values control CRD handling:

| value | default | effect |
| --- | --- | --- |
| `crds.enabled` | `true` | Install and upgrade the CRDs with the chart. Set `false` to manage them out of band. |
| `crds.keep` | `false` | When `true`, annotates the CRDs with `helm.sh/resource-policy: keep` so they survive `helm uninstall`. |
