pipeline {
    agent any

    environment {
        REGISTRY = 'registry.poderiaserseudominio.uk/adnangonzaga'
        IMAGE_NAME = 'sghss-api'
        IMAGE_TAG = "$GIT_COMMIT"
        KUBE_CONFIG = credentials('kubeconfig-credentials-id')
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build Docker Image') {
            steps {
                script {
                    docker.build("${REGISTRY}/${IMAGE_NAME}:${IMAGE_TAG}")
                }
            }
        }

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
        // stage('Deploy to Kubernetes') {
        //     steps {
        //         withCredentials([file(credentialsId: 'kubeconfig-credentials-id', variable: 'KUBECONFIG')]) {
        //             sh '''
        //                 kubectl apply -f k8s/db/deployment.yaml --kubeconfig=$KUBECONFIG
        //                 kubectl apply -f k8s/db/service.yaml --kubeconfig=$KUBECONFIG
        //                 kubectl rollout status deployment/sghss-db --kubeconfig=$KUBECONFIG
        //             '''
        //         }
        //     }
        // }
        // stage('Deploy API') {
        //     steps {
        //         withCredentials([file(credentialsId: 'kubeconfig-credentials-id', variable: 'KUBECONFIG')]) {
        //             sh '''
        //                 kubectl apply -f k8s/api/deployment.yaml --kubeconfig=$KUBECONFIG
        //                 kubectl apply -f k8s/api/service.yaml --kubeconfig=$KUBECONFIG
        //                 kubectl rollout status deployment/sghss-api --kubeconfig=$KUBECONFIG
        //             '''
        //         }
        //     }
        // }
        
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