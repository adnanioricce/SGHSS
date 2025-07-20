pipeline {
    agent any

    environment {
        REGISTRY = 'registry.poderiaserseudominio.uk/adnangonzaga'
        IMAGE_NAME = 'sghss-api'
        IMAGE_TAG = "$GIT_COMMIT"
        //KUBE_CONFIG = credentials('kubeconfig-credentials-id')
        KUBE_CONFIG = '/etc/rancher/k3s/k3s.yaml'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build Api Docker Image') {
            steps {
                script {
                    // Build a docker image using the Dockerfile in docker/Api.Dockerfile
                    def dockerFile = 'docker/Api.Dockerfile'
                    docker.build("${REGISTRY}/${IMAGE_NAME}:${IMAGE_TAG}", "-f ${dockerFile} .")
                    // Alternatively, if you want to use the default Dockerfile in the root directory
                    // docker.build("${REGISTRY}/${IMAGE_NAME}:${IMAGE_TAG}")
                }
            }
        }
        stage('Build Database Docker Image') {
            steps {
                script {
                    // Build a docker image for the database
                    def dbDockerFile = 'docker/Db.Dockerfile'
                    docker.build("${REGISTRY}/sghss-db:${IMAGE_TAG}", "-f ${dbDockerFile} .")
                }
            }
        }
        // stage('Run Tests') {
        //     steps {
        //         script {
        //             // Run tests inside the Docker container
        //             docker.image("${REGISTRY}/${IMAGE_NAME}:${IMAGE_TAG}").inside {
        //                 sh 'dotnet test --no-build --verbosity normal'
        //             }
        //         }
        //     }
        // }
        stage('Push Docker Image') {
            steps {
                script {
                    docker.withRegistry("https://${REGISTRY}", 'docker-credentials-id') {
                        docker.image("${REGISTRY}/${IMAGE_NAME}:${IMAGE_TAG}").push()
                    }
                }
            }
        }
        //TODO: Uncomment and configure the following stages for deployment to Kubernetes
        stage('Deploy database changes to k8s cluster') {
          steps {
            //withCredentials([file(credentialsId: 'kubeconfig-credentials-id', variable: 'KUBECONFIG')]) {
              sh '''
                KUBECONFIG=/home/dev/.kube/jenkins.config
                # Replace the ${TAG-latest} with the IMAGE_TAG generated on the pipeline
                sed -i "s/\${TAG-latest}/${IMAGE_TAG}/g" k8s/db/deployment.yaml
                kubectl apply -f k8s/db/deployment.yaml --kubeconfig=$KUBECONFIG
                kubectl apply -f k8s/db/service.yaml --kubeconfig=$KUBECONFIG
                kubectl rollout status deployment/sghss-db --timeout=30s -n sghss --kubeconfig=$KUBECONFIG
              '''
            //}
          }
        }
        stage('Deploy API') {
            steps {
                //withCredentials([file(credentialsId: 'kubeconfig-credentials-id', variable: 'KUBECONFIG')]) {
                    sh '''
                        KUBECONFIG=/home/dev/.kube/jenkins.config
                        sed -i "s/\${TAG-latest}/${IMAGE_TAG}/g" k8s/api/deployment.yaml
                        kubectl apply -f k8s/api/deployment.yaml --kubeconfig=$KUBECONFIG
                        kubectl apply -f k8s/api/service.yaml --kubeconfig=$KUBECONFIG
                        kubectl apply -f k8s/api/ingress.yaml --kubeconfig=$KUBECONFIG
                        kubectl rollout status deployment/sghss-api --timeout=30s -n sghss --kubeconfig=$KUBECONFIG
                    '''
                //}
            }
        }
        
        // stage('Deploy to Kubernetes') {
        //     steps {
        //         withCredentials([file(credentialsId: 'kubeconfig-credentials-id', variable: 'KUBECONFIG')]) {
        //             sh '''
        //                 kubectl set image deployment/${IMAGE_NAME} ${IMAGE_NAME}=${REGISTRY}/${IMAGE_NAME}:${IMAGE_TAG} --kubeconfig=$KUBECONFIG
        //                 kubectl rollout status deployment/${IMAGE_NAME} --kubeconfig=$KUBECONFIG
        //             '''
        //         }
        //     }
        // }
    }
}
