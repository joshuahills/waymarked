FROM eclipse-temurin:21-jre

RUN mkdir -p /graphhopper /data

ADD https://repo1.maven.org/maven2/com/graphhopper/graphhopper-web/11.0/graphhopper-web-11.0.jar /graphhopper/graphhopper-web.jar

# Copy custom routing profile models into container
COPY data/foot.json /data/foot.json
COPY data/hike.json /data/hike.json
COPY data/foot_elevation.json /data/foot_elevation.json

WORKDIR /graphhopper

EXPOSE 8989

ENTRYPOINT ["sh", "-c", "exec java ${JAVA_OPTS} -jar /graphhopper/graphhopper-web.jar \"$@\"", "sh"]

CMD ["server", "/data/config.yml"]
