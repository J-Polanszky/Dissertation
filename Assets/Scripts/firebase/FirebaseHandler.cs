using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseHandler : MonoBehaviour
{
    public static FirebaseHandler Instance;

    private FirebaseDatabase _database;
    private DatabaseReference _playerRef;
    private string playerID;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                _database = FirebaseDatabase.GetInstance(app,
                    "https://dissertation-eda35-default-rtdb.europe-west1.firebasedatabase.app/");
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + task.Result);
            }
        });
    }

    async Task<string> CheckValue(DatabaseReference reference)
    {
        DataSnapshot snapshot = await reference.GetValueAsync();
        try
        {
            if (snapshot.Value == null)
                return null;
            return snapshot.GetRawJsonValue();
        }
        catch (NullReferenceException e)
        {
            Debug.LogError("Failed to load value: " + e);
            return null;
        }
    }

    public void SendEndOfGameEvent(string playerID, string playTestName, EndOfGame endOfGame)
    {
        if (this.playerID != playerID)
            _playerRef = _database.GetReference(playerID);

        _playerRef ??= _database.GetReference(playerID);

        this.playerID = playerID;

        DatabaseReference playTestRef = _playerRef.Child(playTestName).Child("EndOfGame");

        string endOfGameJson = JsonUtility.ToJson(endOfGame);

        // Check if value already exists
        CheckValue(playTestRef).ContinueWith(readTask =>
        {
            void WriteLocally(AggregateException e = null)
            {
                string errormsg = e == null ? "Value already exists" : e.ToString();
                Debug.LogError($"Failed to save end of game event: {errormsg}");

                string[] directories = { playerID, playTestName };
                string currentPath = Application.persistentDataPath;
                foreach (string directory in directories)
                {
                    currentPath = Path.Combine(currentPath, directory);
                    if (!Directory.Exists(currentPath))
                    {
                        Directory.CreateDirectory(currentPath);
                    }
                }

                string path = Application.persistentDataPath + "/" + playerID + "/" + playTestName + "/EndOfGame.json";
                File.WriteAllText(path, endOfGameJson);
            }

            if (readTask.IsFaulted)
            {
                // Write locally
                WriteLocally(readTask.Exception);
                return;
            }

            if (readTask.Result != null)
            {
                // Write locally
                WriteLocally();
                return;
            }

            playTestRef.SetValueAsync(endOfGameJson).ContinueWith(writeTask =>
            {
                if (writeTask.IsFaulted)
                {
                    // Write locally
                    WriteLocally(writeTask.Exception);
                }
            });
        });
    }

    public void SendTimestampEvent(string playerID, string playTestName, string timestamp,
        TimeStampEvent timeStampEvent)
    {
        if (this.playerID != playerID)
            _playerRef = _database.GetReference(playerID);

        _playerRef ??= _database.GetReference(playerID);

        this.playerID = playerID;

        DatabaseReference playTestRef = _playerRef.Child(playTestName).Child("TimeStamps").Child(timestamp);

        string timeStampJson = JsonUtility.ToJson(timeStampEvent);

        // Check if value already exists
        CheckValue(playTestRef).ContinueWith(readTask =>
        {
            void WriteLocally(AggregateException e = null)
            {
                string errormsg = e == null ? "Value already exists" : e.ToString();
                Debug.LogError($"Failed to save end of game event: {errormsg}");

                string[] directories = { playerID, playTestName, "Timestamps" };
                string currentPath = Application.persistentDataPath;
                foreach (string directory in directories)
                {
                    currentPath = Path.Combine(currentPath, directory);
                    if (!Directory.Exists(currentPath))
                    {
                        Directory.CreateDirectory(currentPath);
                    }
                }

                string path = Application.persistentDataPath + "/" + playerID + "/" + playTestName + "/Timestamps/" +
                              timestamp + ".json";
                File.WriteAllText(path, timeStampJson);
            }

            if (readTask.IsFaulted)
            {
                // Write locally
                WriteLocally(readTask.Exception);
                return;
            }

            if (readTask.Result != null)
            {
                // Write locally
                WriteLocally();
                return;
            }

            playTestRef.SetValueAsync(timeStampJson).ContinueWith(writeTask =>
            {
                if (writeTask.IsFaulted)
                {
                    // Write locally
                    WriteLocally(writeTask.Exception);
                }
            });
        });
    }
}