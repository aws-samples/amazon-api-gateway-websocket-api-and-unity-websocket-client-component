/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

namespace Amazon.Unity
{

    public class GameBoard : MonoBehaviour
    {
        #region NESTED ENUMS
        public enum Direction { Up, Down, Left, Right };
        #endregion

        #region PROPERTIES
        public int width, height;

        public float tileSize;

        public GameObject tilePrefab;

        public GameObject playerPrefab;

        public Light directionalLight;

        public List<Color> lightColors = new List<Color>();

        public GameObject enemySearchEffectPrefab;

        public GameObject playerSearchEffectPrefab;

        public GameObject startGameUI;

        public TMP_InputField PlayerNameInput;

        public GameObject titleGameObject;

        public TextMeshProUGUI messageText;

        private GameObject tileContainer;

        private Tile[] tiles;

        private GameObject player;

        private int playerLocation;

        private ParticleSystem enemySearchParticleSystem;

        private ParticleSystem playerSearchParticleSystem;

        private Coroutine messageCoroutine;

        private string playerName;

        private string[] randomPlayerNames = new string[] { "Broken Chair", "Dusty Sofa", "Tilty Table", "Crooked Lampshade" };

        #endregion

        #region UNITY LIFECYCLE METHODS
        // Start is called before the first frame update
        void Start()
        {
            RandomizeLight();
            BuildGameBoard();
            SpawnEffects();
            AmazonCognitoManager.Authenticated += AmazonCognitoManager_Authenticated;
        }
        #endregion

        #region PRIVATE METHODS

        private void AmazonCognitoManager_Authenticated()
        {
            startGameUI.SetActive(true);
        }

        private void BuildGameBoard()
        {
            tileContainer = new GameObject("Tile Container");
            tileContainer.transform.parent = transform;

            tiles = new Tile[width * height];

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Vector3 newTilePosition = new Vector3(j * tileSize, 0f, i * tileSize);
                    GameObject tileGameObject = Instantiate(tilePrefab, newTilePosition, Quaternion.identity, tileContainer.transform);
                    tileGameObject.transform.localScale = tileSize * Vector3.one;
                    tiles[i * width + j] = tileGameObject.GetComponent<Tile>();
                }
            }

            tileContainer.transform.position += new Vector3(tileSize * (0.5f - width / 2f), 0f, tileSize * (0.5f - height / 2f));
        }

        private void Move()
        {
            // Get random adjacent tile to broadcast
            bool valid = false;
            int index = playerLocation;

            int count = width * height;
            while (!valid)
            {
                int heightShift = Random.Range(-1, 2);
                
                int heightShiftIndex = playerLocation + heightShift * width;

                if(heightShiftIndex >= count || heightShiftIndex < 0)
                {
                    continue;
                }

                int widthShift = Random.Range(-1, 2);
                if(playerLocation % width == 0 && widthShift == -1)
                {
                    continue;
                }

                if(playerLocation % width == width -1 && widthShift == 1)
                {
                    continue;
                }
                
                index = playerLocation + widthShift + heightShift * width;

                if (index > -1 && index < width * height)
                {
                    valid = true;
                }
            }

            AmazonAPIGatewayWebSocketManager.Instance.SendActionMessage(
                JsonUtility.ToJson(
                    new WebSocketDemoMessageParser.Message("move", index.ToString())
                )
            );
        }

        private void RandomizeLight()
        {
            int lightIndex = Random.Range(0, lightColors.Count);
            directionalLight.color = lightColors[lightIndex];
        }

        private void SpawnEffects()
        {
            GameObject enemyEffectGO = Instantiate(enemySearchEffectPrefab);
            enemySearchParticleSystem = enemyEffectGO.GetComponent<ParticleSystem>();
            enemySearchParticleSystem.Stop();

            GameObject playerEffectGO = Instantiate(playerSearchEffectPrefab);
            playerSearchParticleSystem = playerEffectGO.GetComponent<ParticleSystem>();
            playerSearchParticleSystem.Stop();
        }

        private void SpawnPlayer()
        {
            playerLocation = Random.Range(0, width * height);

            player = Instantiate(playerPrefab, tiles[playerLocation].transform.position, Quaternion.identity);
            player.GetComponent<Player>().GameBoard = this;
            player.transform.localScale = tileSize * Vector3.one;

            AmazonAPIGatewayWebSocketManager.Instance.SendActionMessage(
                JsonUtility.ToJson(
                    new WebSocketDemoMessageParser.Message("playerSpawned", playerName)
                )
            );
        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Check if the player can move
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool CheckMove(Direction direction, out Vector3 position)
        {
            switch (direction)
            {
                case Direction.Down:
                    if (playerLocation > width - 1)
                    {
                        playerLocation -= width;
                        position = tiles[playerLocation].transform.position;
                        Move();
                        return true;
                    }
                    else
                    {
                        position = Vector3.zero;
                        return false;
                    }
                case Direction.Left:
                    if (playerLocation % width > 0)
                    {
                        playerLocation--;
                        position = tiles[playerLocation].transform.position;
                        Move();
                        return true;
                    }
                    else
                    {
                        position = Vector3.zero;
                        return false;
                    }
                case Direction.Right:
                    if (playerLocation % width < width - 1)
                    {
                        playerLocation++;
                        position = tiles[playerLocation].transform.position;
                        Move();
                        return true;
                    }
                    else
                    {
                        position = Vector3.zero;
                        return false;
                    }
                case Direction.Up:
                    if (playerLocation < width * height - width)
                    {
                        playerLocation += width;
                        position = tiles[playerLocation].transform.position;
                        Move();
                        return true;
                    }
                    else
                    {
                        position = Vector3.zero;
                        return false;
                    }
                default:
                    position = Vector3.zero;
                    return false;
            }
        }

        /// <summary>
        /// Game over - the player was tagged
        /// </summary>
        public void GameOver()
        {
            RandomizeLight();
            Destroy(player);
            playerLocation = -1;
            titleGameObject.SetActive(true);
            startGameUI.SetActive(true);
        }

        /// <summary>
        /// Join the current game
        /// </summary>
        public void JoinGame()
        {
            startGameUI.SetActive(false);
            titleGameObject.SetActive(false);
            if(playerName == "" || playerName == null)
            {
                playerName = randomPlayerNames[Random.Range(0, randomPlayerNames.Length)];
            }
            SpawnPlayer();
            RandomizeLight();
        }

        /// <summary>
        /// Show player joined message
        /// </summary>
        /// <param name="name"></param>
        public void PlayerJoinedMessage(string name)
        {
            if(messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
            }

            messageCoroutine = StartCoroutine(ShowMessage($"{name} joined."));
        }

        /// <summary>
        /// Show player tagged message
        /// </summary>
        /// <param name="name"></param>
        public void PlayerTaggedMessage(string name)
        {
            if (messageCoroutine != null)
            {
                StopCoroutine(messageCoroutine);
            }

            messageCoroutine = StartCoroutine(ShowMessage($"{name} was tagged."));
        }
        
        /// <summary>
        /// Search for other players
        /// </summary>
        public void Search()
        {
            playerSearchParticleSystem.Stop();
            playerSearchParticleSystem.transform.position = tiles[playerLocation].transform.position + 0.1f * Vector3.up;
            playerSearchParticleSystem.Play();

            AmazonAPIGatewayWebSocketManager.Instance.SendActionMessage(
                    JsonUtility.ToJson(new WebSocketDemoMessageParser.Message("search", playerLocation.ToString())));
        }

        /// <summary>
        /// Trigger an active detection ! marker and test if local player is tagged
        /// </summary>
        /// <param name="index"></param>
        public void TriggerActiveDetection(int index)
        {
            tiles[index].SetActiveDetection();

            enemySearchParticleSystem.Stop();
            enemySearchParticleSystem.transform.position = tiles[index].transform.position + 0.1f * Vector3.up;
            enemySearchParticleSystem.Play();


            // Check if the player was found
            if (index == playerLocation)
            {
                // Found!
                AmazonAPIGatewayWebSocketManager.Instance.SendActionMessage(
                    JsonUtility.ToJson(
                        new WebSocketDemoMessageParser.Message("playerFound", playerName)
                    ));

                GameOver();
            }
        }

        /// <summary>
        /// Trigger a passive detection ? marker
        /// </summary>
        /// <param name="index"></param>
        public void TriggerPassiveDetection(int index)
        {
            tiles[index].SetPassiveDetection();

        }

        public void UpdatePlayerName()
        {
            playerName = PlayerNameInput.text;
        }

        #endregion

        #region PRIVATE COROUTINES

        private IEnumerator ShowMessage(string message)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);

            yield return new WaitForSeconds(2f);

            messageText.gameObject.SetActive(false);

        }
        #endregion

    }
}