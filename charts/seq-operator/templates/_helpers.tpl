{{/* vim: set filetype=mustache: */}}
{{/*
Expand the name of the chart.
*/}}
{{- define "auth0-operator.name" -}}
  {{- default .Chart.Name | trunc 63 | trimSuffix "-" -}}
{{- end -}}


{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "auth0-operator.chart" -}}
  {{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Common labels for operator
*/}}
{{- define "auth0-operator.labels" -}}
helm.sh/chart: {{ include "auth0-operator.chart" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- range $key, $val :=  .Values.operator.additionalLabels }}
{{ $key }}: {{ $val | quote }}
{{- end }}
{{- end -}}

{{/*
Selector labels Operator
*/}}
{{- define "auth0-operator.selectorLabels" -}}
app.kubernetes.io/name: {{ include "auth0-operator.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end -}}
