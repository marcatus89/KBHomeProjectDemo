docker stop sqlserver
docker rm sqlserver

docker run --platform linux/amd64 \
  -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=268191Thiena@" \
  -p 1433:1433 \
  --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest