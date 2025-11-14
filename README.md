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
