using UnityEngine;

[CreateAssetMenu(menuName = "Character Config", fileName = "CharacterConfig")]
public class CharacterConfig : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;

    [Header("Traits")]
    public bool is_fireproof;
    public bool can_swim;
}
