#!/bin/sh
docker run -it --rm -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Passw0rd!' \
   -p 1433:1433 --name ring-hash.sql \
   mcr.microsoft.com/mssql/server:2017-latest
