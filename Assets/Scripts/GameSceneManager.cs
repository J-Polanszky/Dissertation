using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
   public static GameSceneManager instance;
   
   void Awake()
   {
      if (instance == null)
      {
         instance = this;
         DontDestroyOnLoad(gameObject);
      }
      else
      {
         Destroy(gameObject);
      }
   }
   
   public void StartGame()
   {
      throw new System.NotImplementedException();
   }
}
