using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManagerController : MonoBehaviour {

  #region Dev Info
    [TextArea(3,6)]
    [SerializeField] private string ClassInfo = "";
  #endregion

  #region ref
    public static PlayerManagerController instance;
    StaminaBarPlayer staminaBarPlayer;
    PlayerCollision playerCollision;
    NPCDialogue npcDialogue;
    PlayerSuperRun playerSuperRun;

  #endregion
  //@TODO: check i dont think we need this
  #region dialogue setup
  // Add a list to track NPCs during runtime
  private List<NPCDialogue> npcDialogues = new List<NPCDialogue>();
  #endregion

  #region Control States
    [Space]
    [Header("Control States")]
    public PlayerState currentState;
    public enum PlayerState {
      Still, Idle, Dialogue, Running, SecondaryWeaponInventoryOpening,
      SuperRunning, Jumping, Duck, DuckDropDown, Climb, Mounting,
      Falling, Dropdown,
      Attacking,
      AirAttacking,
      AirGliding,
      BackDashing,
      TeleportScene,
      Hurt,
      #region Actions
      Interactable,
      #endregion

      #region Skills
      Hanger, HangerJumpLaunch, FlashStep, FlashStepHangerLaunch, Absorb,
      #endregion
      Dead
    }

  #endregion

  #region Bool Checks 
    public bool isInDialogueUpdateFSM = false;
    private bool isSecondaryWeaponInventoryActive = false;
    public bool playerInteractY = false;
  #endregion

  #region Enablers
  [Header("Enablers Check")]
    public bool isTeleporting;
    public bool enablerJump;
    [Space]
    public bool stopVelocity;
    public bool stopVelocityX;
    public bool stopVelocityY;
    public bool canRun;
    public bool teleportingZ;
    public bool teleportingNextDoor;
    public bool playingCinematic;
    public bool flashStepStopInput;
  #endregion

  void Start() {
    instance = this;

    playerCollision = GetComponent<PlayerCollision>();
    npcDialogue = FindObjectOfType<NPCDialogue>();
    playerSuperRun = GetComponent<PlayerSuperRun>();
    currentState = PlayerState.Idle;
  }

  void Update() {
    switch (currentState) {

      case PlayerState.Still:
        StillState();
        break;

      case PlayerState.Idle:
        IdleState();
        break;

      case PlayerState.Running:
        RunningState();
        break;

      case PlayerState.Jumping:
        JumpingState();
        break;

      case PlayerState.SuperRunning:
        SuperRunState();
        break;

      case PlayerState.Falling:
        FallingState();
        break;

      case PlayerState.Attacking:
        AttackState();
        break;

      case PlayerState.AirAttacking:
        AttackAirState();
        break;

      case PlayerState.BackDashing:
        BackDash();
        break;

      case PlayerState.Duck:
        DuckState();
        break;

      case PlayerState.DuckDropDown:
        DuckDropDownState();
        break;

      case PlayerState.Hanger:
        HangerState();
        break;

      case PlayerState.HangerJumpLaunch:
        HangerStateJumpLaunch();
        break;

      case PlayerState.FlashStep:
        FlashStepHangerState();
        break;
      
      case PlayerState.FlashStepHangerLaunch:
        FlashStepLaunchState();
        break;

      case PlayerState.Absorb:
        AbsorbState();
        break;

      case PlayerState.Climb:
        ClimbState();
        break;

      case PlayerState.SecondaryWeaponInventoryOpening:
        SecondaryWeaponInventoryOpen();
        break;

      case PlayerState.TeleportScene:
        TeleportSceneState();
        break;

      case PlayerState.Interactable:
        InteractableState();
        break;

      case PlayerState.Mounting:
        MountingState();
        break;

      default:
        break;
    }

    #region Always Running Functions
    
      UpdateDebugManager();

      CheckTransitionToHangerState();
      CheckTransitionToSecretWallAbsorb();
      CheckTransitionToFlashStepState();
      CheckClimbState();
      CheckInteractableState();
      CheckMountingState();

    #endregion

  }

  #region FSM Functions
    void StillState() {
      //This state its used in elevators, dialgoues...
      //Debug.Log("Still State");
    }
    void IdleState() {
      //Debug.Log("Idle State");
      StopVelocityX();
      //StopVelocityY();
      //We must be GROUNDED To go other states
      //PlayerJump.instance.Land();
      PlayerJump.instance.isJumping = false;
      
      if (PlayerCollision.instance.isGrounded 
      || PlayerCollision.instance.isGroundOneWayPlatform 
      && !PlayerJump.instance.isJumping ) {
        
        // STOP THE PLAYER IF ON A SLOPE AND NOT MOVING HORIZONTALLY
        //if (PlayerCollision.instance.isSlope && Mathf.Abs(PlayerInput.instance.horizontalInput) == 0) {
        //    StopVelocityY();
        //}
        
        //JUMP STATE
      
        if (PlayerInput.instance.jumpButton || PlayerInput.instance.XboxA) {
          PlayerJump.instance.Jump();
          currentState = PlayerState.Jumping;
          return;
        }
        //RUN STATE
        if (Mathf.Abs(PlayerInput.instance.horizontalInput) > 0) {
          currentState = PlayerState.Running;
          return; 
        }

        if (PlayerInput.instance.attackButton || PlayerInput.instance.XboxX) {
          PlayerAttack.instance.AttackBasic();
          currentState = PlayerState.Attacking; // Transition to Attacking state
          return;
        }    

        //DUCK STATE
        //if (PlayerInput.instance.horizontalInput == 0) {
          if (!PlayerDuck.instance.isDucking && PlayerInput.instance.downArrow 
          || PlayerInput.instance.verticalInput < 0) {
            currentState = PlayerState.Duck;
          }          
      }

      //NOT GROUNDED//

      //FALLING STATE
      if (PlayerCollision.instance.isDescending 
      && !PlayerCollision.instance.isGrounded 
      && !PlayerCollision.instance.isGroundOneWayPlatform) {
        currentState = PlayerState.Falling;
          return;
      }
    
      //BACKDASH STATE    
      if (StaminaBarPlayer.instance != null) {
        if (StaminaBarPlayer.instance.CanPerformAction(PlayerBackDash.instance.backdashStaminaCost)) {
          if (PlayerInput.instance.XboxLeftBumper || Input.GetKeyDown(KeyCode.Q)) {
            currentState = PlayerState.BackDashing;
          }
        }
      } else {
        Debug.Log("StaminaBar not found");
      }

    }
    void RunningState() {
      //Debug.Log("Running State");

      float threshold = 0.1f;

      // Check if the horizontal input is close to zero
      if (Mathf.Abs(PlayerInput.instance.horizontalInput) < threshold) {
        currentState = PlayerState.Idle;
        StopVelocityX();
        //Attack Input
      } else {
        PlayerMove.instance.Run();
      }

      //Super Run
      //TODO Fix this.
      //Check if we are running first
      if (Input.GetKey(KeyCode.V) || PlayerInput.instance.leftTrigger == 1) {
        currentState = PlayerState.SuperRunning;
      }

      if (PlayerCollision.instance.isGrounded 
      || PlayerCollision.instance.isGroundOneWayPlatform
       
      && !PlayerJump.instance.isJumping) {
        // Trigger jump immediately
        if (PlayerInput.instance.jumpButton || PlayerInput.instance.XboxA) {
          PlayerJump.instance.Jump();
          currentState = PlayerState.Jumping;
          return;
        }
      }

      // Check for jump button release to allow mid-air release
      if (PlayerInput.instance.jumpButtonRelease && PlayerJump.instance.isJumping) {
        // Apply mid-air release directly in the Jump method
        PlayerJump.instance.Jump();
        currentState = PlayerState.Jumping;
      }
        
      if (PlayerInput.instance.attackButton) {
          PlayerAttack.instance.AttackBasic();
          currentState = PlayerState.Attacking;
      }

      if (!playerCollision.isGrounded && !playerCollision.isGroundOneWayPlatform) {
        currentState = PlayerState.Falling;
      }

     

    }
    void JumpingState() {
      //Debug.Log("Jumping State");
      PlayerJump.instance.Jump();
      PlayerMove.instance.Run();
      
      if (PlayerCollision.instance.isAscending
        && PlayerInput.instance.jumpButtonRelease) {
        currentState = PlayerState.Falling;
        return;
      }

      //Peak Jump and descending
      if ((Mathf.Approximately(PlayerMove.instance.rb.velocity.y, 0f) || PlayerCollision.instance.isDescending) 
        && !PlayerCollision.instance.isGrounded) {
        currentState = PlayerState.Falling;
        //Debug.Log("Peak or descending, transitioning to Falling state");
        return;
      }

      //TODO Polish this one for combat
      //AIR ATTACK
      if (!playerCollision.isGrounded) {
        if (PlayerInput.instance.attackButton) {
          PlayerAttack.instance.AttackAirBasic();
          currentState = PlayerState.AirAttacking; // Transition to AirAttacking state
        } /*else if (PlayerJump.instance.isFalling || PlayerInput.instance.jumpButtonRelease) {
          currentState = PlayerState.Falling;
        }*/
      }

    }
    void FallingState() {
      //Debug.Log("Falling State");
      PlayerMove.instance.Run();
      PlayerDuck.instance.isDucking = false;
      
      // If ground or one-way ground, go back to Idle  
      if (PlayerCollision.instance.isGrounded || PlayerCollision.instance.isGroundOneWayPlatform) {
        // Return to Idle state even if the jump button is still held
        currentState = PlayerState.Idle;

      }

      //AIR ATTACK
      if (!playerCollision.isGrounded || !playerCollision.isGroundOneWayPlatform) {
        if (PlayerInput.instance.attackButton) {
          PlayerAttack.instance.AttackAirBasic();
          currentState = PlayerState.AirAttacking; // Transition to AirAttacking state
        } else if (PlayerJump.instance.isFalling || PlayerInput.instance.jumpButtonRelease) {
          currentState = PlayerState.Falling;
        }
      
      }
    }
    void SuperRunState()  {
      //Debug.Log("SuperRun State");

      //Debug Settings 
      playerSuperRun.SuperRunActivated();

    }
    void AttackState() {
      //Debug.Log("Attack State");
        
      StopVelocityX();

      //BackDash
      if (StaminaBarPlayer.instance != null) {
        if (StaminaBarPlayer.instance.CanPerformAction(PlayerBackDash.instance.backdashStaminaCost)) {
          if (PlayerInput.instance.XboxLeftBumper || Input.GetKeyDown(KeyCode.Q)) {
            PlayerAttack.instance.SetStopAttack();
            currentState = PlayerState.BackDashing;
          }
        }
      } 
    }
    void AttackAirState() {
      //Debug.Log("Air Attack State");
      PlayerJump.instance.Jump();
      PlayerMove.instance.Run();

      /*if (PlayerCollision.instance.isDescending) {
        if (PlayerCollision.instance.onGround 
        || PlayerCollision.instance.onGroundOneWayPlatform) {
          currentState = PlayerState.Idle;
        }
      }*/
    }
    void BackDash() {
      //Debug.Log("BackDash");

      //Initiate Back Dash Skill
      if (!PlayerBackDash.instance.isBackdashing) {
        PlayerBackDash.instance.InitiateBackdash();
      }
    }
    void DuckState() {
      //Debug.Log("DuckState");

      PlayerDuck.instance.Duck();      

      //Duck Exit when Jump on Ground
      if (PlayerCollision.instance.isGrounded || PlayerCollision.instance.isGroundOneWayPlatform) {
        if (PlayerInput.instance.jumpButton && PlayerDuck.instance.isDucking) {
            PlayerDuck.instance.isDucking = false;
            PlayerJump.instance.Jump();
            currentState = PlayerState.Jumping;
            PlayerDuck.instance.StopDuckAndJump();
          }
        }
      //For One Way Platform, logic is inside the 
      //(oneWayPlatform) gameboject

      /*if (!PlayerCollision.instance.isGrounded || !PlayerCollision.instance.isGroundOneWayPlatform) {
        if (PlayerCollision.instance.isDescending) {
          currentState = PlayerState.Falling;
        }      
      }*/

    }
    void DuckDropDownState() {
      Debug.Log("DuckDropDown");
    }
    public void HangerState() {
      //Debug.Log("HangerState");
      //Logic is in Player > HangerSkill script
    }
    public void MountingState() {
      //Debug.Log("Mounting State");

      //if (PlayerMounts.instance.isMounted) {
      //  PlayerMove.instance.MovingHangerHorizontalMove();
      //}
      if (!PlayerMounts.instance.isMounted) {
        Debug.Log("Exit Mount State");
        currentState = PlayerState.Idle;
      }

    }
    public void HangerStateJumpLaunch() {
      //Debug.Log("HangerStateJump");
      PlayerMove.instance.Run();

      if (PlayerCollision.instance.isAscending
        && PlayerInput.instance.jumpButtonRelease) {
        currentState = PlayerState.Falling;
        return;
      }

      // check for peak ascending
      if ((Mathf.Approximately(PlayerMove.instance.rb.velocity.y, 0f) 
        || PlayerCollision.instance.isDescending) 
        && !PlayerCollision.instance.isGrounded) {
        currentState = PlayerState.Falling;
        
        //Debug.Log("Peak or descending, transitioning to Falling state");
        return;
      }


    }
    public void FlashStepHangerState() {
      //Debug.Log("HangerState");
      //Activate from func (CheckTransitionToHangerState())
      FlashStepHanger.instance.FlashStepHangerUpdate();

      //if (SkillHanger.instance != null 
      //&& SkillHanger.instance.GetSelectedDirectionVector() != Vector2.zero) {
        if (PlayerInput.instance.jumpButton && !FlashStepHanger.instance.isLaunching) {
          // Handle the Jump button press logic in SkillHanger
          FlashStepHanger.instance.HandleJumpButtonPress();

          // Check if the player has been launched
          if (FlashStepHanger.instance.isLaunching) {
            currentState = PlayerState.FlashStepHangerLaunch;
          }
          
        }
      //}

    }
    public void FlashStepLaunchState() {
      //Debug.Log("HangerLaunchState");
    
      if (PlayerInput.instance.downArrow) {
        Debug.Log("CANCEL -> HangerLaunchState");
        ActivateIdleState();
      }

      //EXIT Grounded Collision
      if (PlayerCollision.instance.isGrounded || PlayerCollision.instance.isGroundOneWayPlatform) {
        ActivateIdleState();
      }

    }
    public void AbsorbState() {
      Debug.Log("AbsorbState");
      playerInteractY = true;
      //PlayerInteractionButtonsManager.instance.DeactivateYButtonIcon();

      SkillAbsorb.instance.UpdateAbsorbState();

      //FX


    }
    public void ClimbState() {
      //VINEWALKER

      PlayerMove.instance.SetVec2VelocityZero();
      PlayerMove.instance.SetGravityStop();
      
      //VINEWALKER when unlock skill
    
      PlayerMove.instance.ClimbMovement();
      

      #region Exit State
      if (PlayerInput.instance.XboxY && PlayerInput.instance.downArrow) {
        
        SkillClimb.instance.ExitClimbState();
      }
      
      if (PlayerInput.instance.jumpButton && !PlayerInput.instance.downArrow) {
        
        currentState = PlayerState.Jumping;
        PlayerMove.instance.rb.velocity = new Vector2(PlayerMove.instance.rb.velocity.x, PlayerJump.instance.JumpForce);
        PlayerMove.instance.SetGravityNormal();
      }

      if (PlayerCollision.instance.isGrounded) {
        SkillClimb.instance.ExitClimbState();
      }

      #endregion

    }
    public void InteractableState() {
      //Debug.Log("InteractableState");

    }
    void SecondaryWeaponInventoryOpen() {
      //Func to open the Secondary Inventory as a state to control input
      //and subwepaons, this will be use if later we decide to change the 
      //subweapon to a stop pause menu style to change pase  
    }  
    void TeleportSceneState() {
      //Debug.Log("TeleportSceneState");

      StopVelocity();

    }
    public void OnAttackFinished() {
    //Debug.Log("OnAttackFinished");
    
      //Go back to idle if we grounded or falling if we finish air attack
      if (PlayerCollision.instance.isDescending) {
        currentState = PlayerState.Falling;  
      } else {
        currentState = PlayerState.Idle;

      }
    
    }

  #endregion

  #region Debug Manager
    void UpdateDebugManager() {
      //Display current state into the Debug Manager Canvas
      string playerStateInfo = "" + currentState.ToString();
      DebugManager.instance.UpdateDebugPlayerDataState(playerStateInfo);
    }

  #endregion

  #region Setters
  public void ActivateIdleState() {
    currentState = PlayerState.Idle;  
  }
  public void ActivateFallingState() {
    currentState = PlayerState.Falling;
  }
  public void ActivateStillState() {
    currentState = PlayerState.Still;
  }
  public void ActivateJumpState() {
    currentState = PlayerState.Jumping;
  }
  public void ActivateTeleportSceneState() {
    currentState = PlayerState.TeleportScene;
  }
  
  #endregion

  #region Getters 
  public PlayerState GetCurrentState() {
    return currentState;
  }

  #endregion

  #region Always Running Checks
  
  //This functions are to detect collisions with Objects
  //that Trigger a state, always running
  public void CheckTransitionToHangerState() {
    if (PlayerData.instance.learnedHangingSkill) {
      if (PlayerCollision.instance.isHangerSkill) {
        //Debug.Log("PlayerCollision.instance.isHangerSkill");
        currentState = PlayerState.Hanger;
        HangerSkill.instance.HangerState();
      }
    }
  }
  public void CheckTransitionToFlashStepState() {
    if (PlayerCollision.instance.isFlashStepHangerSkill) {
      //Init once
      FlashStepHanger.instance.FlashStepHangerState();
      currentState = PlayerState.FlashStep;
    }
  }
  public void CheckTransitionToSecretWallAbsorb() {
    if (PlayerCollision.instance.isSecretsWalls) {
      //Debug.Log("Display Interaction Button");
      PlayerInteractionButtonsManager.instance.ActivateYButtonIcon();
        if (PlayerInput.instance.XboxY)   {
          PlayerInteractionButtonsManager.instance.DeactivateYButtonIcon();
        //Debug.Log("Absord Darkness");
        currentState = PlayerState.Absorb;
        SkillAbsorb.instance.EnterAbsorbState();
      }
    } else {
      PlayerInteractionButtonsManager.instance.DeactivateYButtonIcon();
    } 
  }
  public void CheckClimbState() {
    //VINEWALKER unlcoked
    if (PlayerData.instance.learnedVinewalker) {
      if (PlayerCollision.instance.isClimbLayer && PlayerInput.instance.XboxY) {
        currentState = PlayerState.Climb;
      }
    }
  }
  public void CheckInteractableState() {
    if (PlayerCollision.instance.isInteractable && PlayerInput.instance.XboxY) {
      currentState = PlayerState.Interactable;
    }
  }
  public void CheckMountingState() {
    if (PlayerCollision.instance.isMount) {
      currentState = PlayerState.Mounting;
    }
  }
  #endregion

  public void InitiateDialogueStateFSM() {
    // Transition to the Dialogue state
    currentState = PlayerState.Dialogue;
  }

  public void RestoreAllControl() {
    PlayerInput.instance.enableAttack = true;
    PlayerInput.instance.enableJump = true;
    PlayerInput.instance.enableDashButton = true;
      
    stopVelocity = false;
    flashStepStopInput = false;
    teleportingZ = false;
    teleportingNextDoor =false;
  }
  //@@@@@@@@@@@@DOUBLE CHECK IF NECESSARY
  public void StopVelocity() {
      Vector2 stopVelocity = new Vector2(0f, 0f);
      PlayerMove.instance.rb.velocity = stopVelocity;
  }
  //@@@@@@@@@@@@DOUBLE CHECK IF NECESSARY
  public void StopVelocityX() {
    Vector2 stopVelocityX = new Vector2(0f,PlayerMove.instance.rb.velocity.y);
    PlayerMove.instance.rb.velocity = stopVelocityX;
  }
  //@@@@@@@@@@@@DOUBLE CHECK IF NECESSARY
  public void StopVelocityY() {
      Vector2 stopVelocityY = new Vector2(PlayerMove.instance.rb.velocity.x, 0f);
      PlayerMove.instance.rb.velocity = stopVelocityY;
  }
    


}
