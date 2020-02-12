# Deployment Steps

## Deploy Azure resources

### Create Group and Initial Resources

`az group create -n ChessTrainer`

`az group deployment create -g ChessTrainer --template-file azuredeploy.json --parameters azuredeploy.parameters.json`

### Configure Resources that aren't Configurable via ARM Templates

`az sql db create -s chesstrainersqlserver -g ChessTrainer -n ChessTrainerDB -e GeneralPurpose --compute-model Serverless --family Gen5 --min-capacity 0.5 --max-size 32GB --auto-pause-delay 480 --capacity 1`

`az sql db show-connection-string --client ado.net -n ChessTrainerDb -s chesstrainersqlserver`

`az storage account show-connection-string -g ChessTrainer -n chesstrainerstorage --key primary`

`az storage queue create -n gameingestionqueue --connection-string <redacted>`

`az storage table create -n gameingestionqueue --connection-string <redacted>`
