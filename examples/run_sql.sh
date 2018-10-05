#!/bin/sh
docker run -it --rm -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Passw0rd!' \
   -p 1433:1434 --name ring-hash.sql \
   -d mcr.microsoft.com/mssql/server:2017-latest