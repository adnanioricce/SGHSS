#!/bin/bash
GIT_COMMIT=$(git rev-parse HEAD)
echo "Building Docker image with commit: $GIT_COMMIT"

docker build -t "registry.poderiaserseudominio.uk/adnangonzaga/sghss-api:$GIT_COMMIT" .
docker stop sghss-api || true
docker rm sghss-api || true
# docker rmi registry.poderiaserseudominio.uk/adnangonzaga/sghss-api || true
docker push "registry.poderiaserseudominio.uk/adnangonzaga/sghss-api:$GIT_COMMIT"
sed -i "s/^\( *image: \).*/\1registry.poderiaserseudominio.uk\/adnangonzaga\/sghss-api:$GIT_COMMIT/" k8s/api/deployment.yaml
sed -i "s/^\( *image: \).*/\1registry.poderiaserseudominio.uk\/adnangonzaga\/sghss-db:$GIT_COMMIT/" k8s/db/deployment.yaml
kubectl apply -f k8s/api/deployment.yaml
kubectl apply -f k8s/api/service.yaml
kubectl apply -f k8s/db/deployment.yaml
kubectl apply -f k8s/db/service.yaml
# docker run -d -p 5000:80 --name sghss-api "registry.poderiaserseudominio.uk/adnangonzaga/sghss-api:$GIT_COMMIT"
