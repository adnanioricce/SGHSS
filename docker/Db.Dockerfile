FROM postgres:16

ENV POSTGRES_USER=postgres
ENV POSTGRES_PASSWORD=senha
ENV POSTGRES_DB=sghss

COPY Db/ /docker-entrypoint-initdb.d/

EXPOSE 5432