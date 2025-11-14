## Manage Seq with seq-operator

### Deploy

```bash {"name":"deploy"}
export KUBECONFIG=${PWD}/.kubeconfig
kind create cluster --name seq-operator
kubectl kustomize . --enable-helm | kubectl apply -f -
```

### Status

```bash {"name":"status"}
kubectl get secret -n seq-operator-example-manage
kubectl get configmap -n seq-operator-example-manage
kubectl get pod -n seq-operator-example-manage
echo "Seq admin password: $(cat seq/password.txt)"
```

### Delete

```bash {"name":"delete"}
kubectl kustomize . --enable-helm | kubectl delete -f -
```
