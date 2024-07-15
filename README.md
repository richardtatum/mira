# MIRA - A BroadcastBox Discord Bot
Mira is a Discord bot for use with the fantastic [BroadcastBox](https://github.com/glimesh/broadcast-box). It allows you to specify which hosts to monitor and then subscribe to different stream keys.

## Prerequisites
Mira requires you to provide a Discord bot token. You can acquire one by visiting the [Discord Applications Portal](https://discord.com/developers/applications/) and creating a new application.

## Add To Server
Coming soon!

## Run this locally
Build and run `Mira` with Docker:
```sh
git clone https://github.com/richardtatum/mira.git mira
cd mira
docker build -t mira .
docker run -e DISCORD__TOKEN='valid-bot-token-here' mira:latest
```
Note that the environment variable `DISCORD__TOKEN` has a double underscore between the two words.

## Usage
Once added, Mira listens for slash commands in any channel. These commands should be automatically populated by Discord into the command pop-up.

### Commands
Mira provides the following slash commands out of the box:

#### /add-host
The `add-host` command allows a user to add a new BroadcastBox host and choose one of the selected polling intervals.

#### /subscribe
The `subscribe` command allows a user to subscribe to changes to a provided stream key on a selected host. When a stream starts on the given host/key, a notification will be sent to the channel the subscription was requested from.

#### /unsubscribe
The `unsubscribe` command allows a user to choose a subscription to remove from the channel from a list of available subscriptions.

#### /list
The `list` command provides a list of all registered hosts and their subscribed keys.

### /remove-host
The `remove-host` command allows a user to choose a host to remove, along with any relevant subscriptions.

## References
- [Discord.NET](https://docs.discordnet.dev/index.html) 
- [BroadcastBox](https://github.com/glimesh/broadcast-box)
