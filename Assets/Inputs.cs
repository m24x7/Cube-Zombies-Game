using UnityEngine;
using UnityEngine.InputSystem;

public class Inputs : MonoBehaviour
{
    [Header("Character Input Values")]
    public Vector2 move;
    public Vector2 look;
    public bool jump;
    public bool sprint;
    public bool attack;
    public bool useItem;
    public bool pauseGame;
    public bool instructions;
    public bool swapSword;
    public bool swapBlock;
    public bool hotBar3;
    public bool hotBar4;

    [Header("Movement Settings")]
    public bool analogMovement;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
    public void OnMove(InputValue value)
    {
        MoveInput(value.Get<Vector2>());
    }

    public void OnLook(InputValue value)
    {
        if (cursorInputForLook)
        {
            LookInput(value.Get<Vector2>());
        }
    }

    public void OnJump(InputValue value)
    {
        JumpInput(value.isPressed);
    }

    public void OnSprint(InputValue value)
    {
        SprintInput(value.isPressed);
    }

    public void OnAttack(InputValue value)
    {
        AttackInput(value.isPressed);
    }

    public void OnUseItem(InputValue value)
    {
        UseItemInput(value.isPressed);
    }
    public void OnSwapSword(InputValue value)
    {
        SwapSword(value.isPressed);
    }
    public void OnSwapBlock(InputValue value)
    {
        SwapBlock(value.isPressed);
    }
    public void OnHotBar3(InputValue value)
    {
        Debug.Log("HotBar3 pressed: " + value.isPressed);
        HotBar3(value.isPressed);
    }
    public void OnHotBar4(InputValue value)
    {
        Debug.Log("HotBar4 pressed: " + value.isPressed);
        HotBar4(value.isPressed);
    }
    public void OnPauseGame(InputValue value)
    {
        PauseGame(value.isPressed);
    }

    public void OnInstructions(InputValue value)
    {
        Instructions(value.isPressed);
    }
#endif


    public void MoveInput(Vector2 newMoveDirection)
    {
        move = newMoveDirection;
    }

    public void LookInput(Vector2 newLookDirection)
    {
        look = newLookDirection;
    }

    public void JumpInput(bool newJumpState)
    {
        jump = newJumpState;
    }

    public void SprintInput(bool newSprintState)
    {
        sprint = newSprintState;
    }

    public void AttackInput(bool newAttackState)
    {
        attack = newAttackState;
    }
    public void UseItemInput(bool newUseItemState)
    {
        useItem = newUseItemState;
    }
    public void SwapSword(bool newSwordState)
    {
        swapSword = newSwordState;
    }
    public void SwapBlock(bool newBlockState)
    {
        swapBlock = newBlockState;
    }
    public void HotBar3(bool newHotBar3State)
    {
        hotBar3 = newHotBar3State;
    }
    public void HotBar4(bool newHotBar4State)
    {
        hotBar4 = newHotBar4State;
    }
    public void PauseGame(bool newPauseState)
    {
        pauseGame = newPauseState;
    }
    public void Instructions(bool newInstructionsState)
    {
        instructions = newInstructionsState;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
