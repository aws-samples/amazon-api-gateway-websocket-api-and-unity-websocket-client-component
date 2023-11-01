/*!
 * Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * SPDX-License-Identifier: MIT-0
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Amazon.Unity
{
    public class Player : MonoBehaviour
    {
        public GameBoard GameBoard { get; set; }

        public float MoveSpeed = 1f;

        private Vector3 targetPosition;
        private bool canMove = true;
        private bool searching = false;
        private float scanTimestamp;
        private float scanCooldown = 1f;

        void Update()
        {
            if (canMove)
            {
                CheckInput();
            }
            else
            {
                if (searching)
                {
                    Scan();
                }
                else
                {
                    MovePlayer();
                }
            }
        }

        private void CheckInput()
        {
            if (GameBoard == null || !canMove)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                TryMoveDirection(GameBoard.Direction.Up);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                TryMoveDirection(GameBoard.Direction.Down);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                TryMoveDirection(GameBoard.Direction.Right);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                TryMoveDirection(GameBoard.Direction.Left);
            }
            else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Keypad0))
            {
                Search();
            }

        }

        private void MovePlayer()
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, MoveSpeed * Time.deltaTime);
            if ((transform.position - targetPosition).sqrMagnitude < 0.0001f)
            {
                transform.position = targetPosition;
                canMove = true;
            }
        }

        private void Scan()
        {
            if (Time.time - scanTimestamp > scanCooldown)
            {
                searching = false;
                canMove = true;
            }
        }

        private void Search()
        {
            GameBoard.Search();
            scanTimestamp = Time.time;
            canMove = false;
            searching = true;
        }

        private void TryMoveDirection(GameBoard.Direction direction)
        {
            if (GameBoard.CheckMove(direction, out Vector3 newPosition))
            {
                targetPosition = newPosition;
                canMove = false;
            }
        }
    }
}