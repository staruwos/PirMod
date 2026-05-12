import discord
from discord.ext import commands, tasks
import aiohttp
import json
import os

# Import the secrets from our new file
from bot_secrets import TOKEN, CHANNEL_ID

# --- CONFIGURATION ---
THUNDERSTORE_URL = "https://thunderstore.io/api/experimental/package/PirTeam/PirMod/"
TODO_FILE = "todos.json"

# --- SETUP ---
intents = discord.Intents.default()
intents.message_content = True  # Required to read messages
bot = commands.Bot(command_prefix="!", intents=intents)

# --- TODO SYSTEM & MIGRATION ---
# Load existing TODOs from file
if os.path.exists(TODO_FILE):
    with open(TODO_FILE, "r") as f:
        todos = json.load(f)
else:
    todos = []

# Give IDs to old tasks that don't have one (Auto-migration)
needs_saving = False
for i, task in enumerate(todos):
    if 'id' not in task:
        task['id'] = i + 1
        needs_saving = True

if needs_saving:
    with open(TODO_FILE, "w") as f:
        json.dump(todos, f, indent=4)

def get_next_id():
    """Finds the highest existing ID and adds 1."""
    if not todos:
        return 1
    return max(task.get('id', 0) for task in todos) + 1

def save_todos():
    with open(TODO_FILE, "w") as f:
        json.dump(todos, f, indent=4)

# --- BOT EVENTS & TASKS ---

@bot.event
async def on_ready():
    print(f'Logged in as {bot.user.name} ({bot.user.id})')
    check_pirmod_updates.start() # Start the background loop

@tasks.loop(minutes=10) # Checks Thunderstore every 10 minutes
async def check_pirmod_updates():
    channel = bot.get_channel(CHANNEL_ID)
    if not channel:
        return

    async with aiohttp.ClientSession() as session:
        async with session.get(THUNDERSTORE_URL) as response:
            if response.status == 200:
                data = await response.json()
                latest = data['latest']
                latest_version = latest['version_number']
                metrics = data.get('metrics', {})
                
                # Check if we have seen this version before
                if not os.path.exists("last_version.txt"):
                    with open("last_version.txt", "w") as f:
                        f.write("")

                with open("last_version.txt", "r") as f:
                    last_seen = f.read().strip()
                
                if latest_version != last_seen:
                    # New update found!
                    embed = discord.Embed(
                        title=f"🚨 PirMod Updated to v{latest_version}! 🚨",
                        url=data['package_url'],
                        color=discord.Color.blue()
                    )
                    embed.add_field(name="Description", value=latest['description'], inline=False)
                    embed.add_field(name="Total Downloads", value=f"{metrics.get('downloads', 0):,}", inline=True)
                    embed.add_field(name="Rating", value=f"⭐ {data.get('rating_score', 0)}", inline=True)
                    
                    if data.get('icon'):
                        embed.set_thumbnail(url=data['icon'])

                    embed.set_footer(text="Check the Thunderstore page for full README and Changelogs.")
                    
                    await channel.send(embed=embed)
                    
                    # Update our local record
                    with open("last_version.txt", "w") as f:
                        f.write(latest_version)

@check_pirmod_updates.before_loop
async def before_update_check():
    await bot.wait_until_ready()

@bot.event
async def on_message(message):
    if message.author == bot.user:
        return

    if bot.user in message.mentions:
        # Clean up the message content
        content = message.content.replace(f'<@{bot.user.id}>', '').strip()
        content = content.replace(f'<@!{bot.user.id}>', '').strip()

        if not content:
            # If it's just a mention, show the list
            await send_todo_list(message.channel)
        else:
            # If there's text, add a new TODO with a unique ID
            new_id = get_next_id()
            todos.append({
                "id": new_id, 
                "author": message.author.name, 
                "task": content
            })
            save_todos()
            await message.reply(f"✅ Added to the TODO list (ID: `{new_id}`): `{content}`")

    await bot.process_commands(message)

# --- COMMANDS ---

async def send_todo_list(destination):
    """A helper to send the TODO list to a specific channel or context."""
    if not todos:
        await destination.send("📝 **The TODO list is currently empty!** Time to get back to Lethal Company.")
        return

    embed = discord.Embed(
        title="📋 PirMod Development - TODO List",
        description="Here are the tasks currently in the queue:",
        color=discord.Color.orange()
    )

    for item in todos:
        item_id = item.get('id', '??')
        embed.add_field(
            name=f"Task #{item_id}", 
            value=f"{item['task']}\n*Submitted by: {item['author']}*", 
            inline=False
        )

    embed.set_footer(text=f"Total tasks: {len(todos)}")
    await destination.send(embed=embed)

@bot.command(name="todo", aliases=["todos"]) # Added an alias so !todo and !todos both work
async def todo_command(ctx):
    """Displays the current PirMod TODO list via command."""
    await send_todo_list(ctx)

@bot.command(name="remove", aliases=["done"])
async def remove_todo(ctx, task_id: int):
    """Removes a TODO by its specific ID."""
    global todos 
    
    original_length = len(todos)
    
    # Keep only the tasks that DO NOT match the requested ID
    removed_task = next((task for task in todos if task.get('id') == task_id), None)
    todos = [task for task in todos if task.get('id') != task_id]
    
    if len(todos) < original_length:
        save_todos()
        await ctx.send(f"Successfully removed **Task #{task_id}**: `{removed_task['task']}`!")
    else:
        await ctx.send(f"❌ Could not find a task with ID `{task_id}`. Run `!todo` to see active IDs.")

@remove_todo.error
async def remove_todo_error(ctx, error):
    if isinstance(error, commands.BadArgument):
        await ctx.send("❌ Please provide a valid Task ID number. Example: `!remove 3`")
    elif isinstance(error, commands.MissingRequiredArgument):
        await ctx.send("❌ You need to tell me which ID to remove! Example: `!remove 3`")

@bot.command(name="last")
async def last_update(ctx):
    """Fetches the details of the most recent PirMod update from Thunderstore."""
    async with aiohttp.ClientSession() as session:
        async with session.get(THUNDERSTORE_URL) as response:
            if response.status == 200:
                data = await response.json()
                latest = data['latest']
                metrics = data.get('metrics', {})

                embed = discord.Embed(
                    title=f"📦 Latest Version: v{latest['version_number']}",
                    url=data['package_url'],
                    description=f"**Description:**\n{latest['description']}",
                    color=discord.Color.blue()
                )
                
                changelog = latest.get('changelog', "View full changelog on Thunderstore.")
                if len(changelog) > 1024:
                    changelog = changelog[:1021] + "..."
                
                embed.add_field(name="What's New", value=changelog, inline=False)
                embed.add_field(name="Total Downloads", value=f"{metrics.get('downloads', 0):,}", inline=True)
                embed.add_field(name="Rating", value=f"⭐ {data.get('rating_score', 0)}", inline=True)
                
                if data.get('icon'):
                    embed.set_thumbnail(url=data['icon'])

                await ctx.send(content="Here is the most recent update info:", embed=embed)
            else:
                await ctx.send("❌ Could not reach Thunderstore. Try again later.")

# Run the bot
bot.run(TOKEN)