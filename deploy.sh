#!/bin/bash

docker build -t registry.poderiaserseudominio.uk/adnangonzaga/sghss-api .
docker stop sghss-api || true
docker rm sghss-api || true
docker run -d -p 5000:80 --name sghss-api sghss-api
