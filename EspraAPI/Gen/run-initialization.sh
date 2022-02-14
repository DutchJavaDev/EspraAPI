apt-get update
apt-get install vim -y

# Wait to be sure that SQL Server came up
sleep 15s

# Run the setup script to create the DB and the schema in the DB
/opt/mssql-tools/bin/sqlcmd -S 127.0.0.1 -U sa -P P@ssw0rd! -i setup/create_database.sql

# Note: make sure that your password matches what is in the Dockerfile
/opt/mssql-tools/bin/sqlcmd -S 127.0.0.1 -U sa -P P@ssw0rd! -d espradb -i setup/create_tables.sql