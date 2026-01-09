set -e

SQLCMD="/opt/mssql-tools18/bin/sqlcmd"

echo "Starting SQL Server..."
/opt/mssql/bin/sqlservr &

echo "Waiting for SQL Server..."
for i in {1..90}; do
  $SQLCMD -S localhost -U sa -P "$SQL_ACCOUNT_SA_PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1 && break
  sleep 2
done

echo "Running init script..."
$SQLCMD -S localhost -U sa -P "$SQL_ACCOUNT_SA_PASSWORD" -C \
  -i /init/initdatabaseaccount.sql -v APP_PASSWORD="$SQL_ACCOUNT_APP_PASSWORD"

wait
