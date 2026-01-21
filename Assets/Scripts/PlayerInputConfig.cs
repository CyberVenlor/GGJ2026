using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Input/Player Input Config", fileName = "PlayerInputConfig")]
public class PlayerInputConfig : ScriptableObject
{
    public InputActionAsset inputActions;
    public string actionMapName = "Player";
    public string moveActionName = "Move";
    public string jumpActionName = "Jump";
}
