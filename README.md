# Kubernetes operator for Seq

## About The Project

This Seq Kubernetes Operator is responsible for managing the lifecycle of Seq resources in a Kubernetes cluster.

### Installation

`helm install -n seq-operator seq-operator oci://ghcr.io/alethic/seq-operator/seq-operator`

## Usage

This is an example for connecting to and bootstrapping a Seq instance deployed with a SEQ_FIRSTRUN_ADMINPASSWORDHASH setting.

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
  firstRunPassword: "1234"

---

apiVersion: seq.k8s.datalust.co/v1alpha1
kind: Instance
metadata:
  name: seqinstance
  namespace: default
spec:
  connections:
  - endpoint: http://localhost:5341
    token:
      secretRef:
        name: seqtoken
  - endpoint: http://localhost:5341
    login:
      secretRef:
        name: seqlogin
  conf: {}
      
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
