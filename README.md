# intel-chan
chat bot for reading a wormhole chain from Tripwire, and subscribing to kill notifications for all systems in the chain, and passing those kills to Groupme.

## usage
requires the following configuration values in secrets.json using Microsoft.Extensions.Configuration.UserSecrets. 
Follows standard appsettings.json format.
* group-me-access-token
* group-me-bot-id

### Location of secrets.json:
- Windows: %AppData%\Microsoft\UserSecrets\\<user_secrets_id>\secrets.json
- Linux / macOS: ~/.microsoft/usersecrets/<user_secrets_id>/secrets.json

user secrets id = 3055347B-1519-4D6D-BACE-727E32EB33E9