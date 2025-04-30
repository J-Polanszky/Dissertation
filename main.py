from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from pymongo import MongoClient
from dotenv import load_dotenv

import datetime
import logging
import os

load_dotenv()

URI = os.getenv("MONGODB_URI")
DB_NAME = os.getenv("MONGODB_DB_NAME")

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI()

# Replace with your actual MongoDB connection string
client = MongoClient(URI)
db = client[DB_NAME]  # Replace with your DB name

# Data: {"difficulty": "Medium", "playerScore": "13", "opponentScore": "6"}


@app.post("/{playtest_type}/{player_id}/end_of_game")
async def end_of_game(playtest_type: str, player_id: str, request: Request):
    data = await request.json()
    collection = db[playtest_type]
    doc = {
        "player_id": player_id,
        "type": "end_of_game",
        "timestamp": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "data": data,
    }
    # Store under: DB > playtest_type > player_id > "end_of_game"
    collection.update_one(
        {"_id": player_id}, {"$set": {f"end_of_game": doc}}, upsert=True
    )
    return JSONResponse({"status": "success"})


# Data: {
#     "difficulty": "Medium",
#     "timePassed": "29",
#     "playerCollectedData": {
#         "timeSpentMining": "20.92308",
#         "timeSpentWTravelling": "8.08",
#         "score": "0",
#         "inventoryUsed": "7",
#         "scoreOfInventory": "13",
#     },
#     "opponentCollectedData": {
#         "timeSpentMining": "11.07692",
#         "timeSpentWTravelling": "17.92",
#         "score": "0",
#         "inventoryUsed": "3",
#         "scoreOfInventory": "6",
#     },
# }


@app.post("/{playtest_type}/{player_id}/{timestamp}")
async def timestamp_event(
    playtest_type: str, player_id: str, timestamp: str, request: Request
):
    data = await request.json()
    collection = db[playtest_type]
    doc = {
        "player_id": player_id,
        "timestamp": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "data": data,
    }
    # Store under: DB > playtest_type > player_id > timestamp
    collection.update_one(
        {"_id": player_id}, {"$set": {f"timestamps.{timestamp}": doc}}, upsert=True
    )
    return JSONResponse({"status": "success"})


@app.post("/{playtest_type}/{player_id}/dda/{timestamp}")
async def dda_event(
    playtest_type: str, player_id: str, timestamp: str, request: Request
):
    data = await request.json()
    collection = db[playtest_type]
    doc = {
        "player_id": player_id,
        "timestamp": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "data": data,
    }
    # Store under: DB > playtest_type > player_id > "dda"
    collection.update_one(
        {"_id": player_id}, {"$set": {f"dda.{timestamp}": doc}}, upsert=True
    )
    return JSONResponse({"status": "success"})


@app.get("/users/{user_id}/isdda")
async def get_isdda(user_id: str):
    logger.info(f"Fetching isDDA for user_id: {user_id}")
    users_collection = db["Users"]  # Make sure your users collection is named "Users"
    user_doc = users_collection.find_one({"_id": user_id})
    logger.info(f"User document: {user_doc}")
    if user_doc and "isDDA" in user_doc:
        return JSONResponse({"isDDA": user_doc["isDDA"]})
    return JSONResponse({"error": "User not found or isDDA not set"}, status_code=404)
