docker build -t counter-image -f Dockerfile .
docker tag counter-image:latest bugblender/dotnettest:latest
docker push bugblender/dotnettest:latest