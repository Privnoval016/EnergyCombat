using UnityEngine;

[CreateAssetMenu(menuName = "Player/Animation Config")]
public class PlayerAnimationConfig : ScriptableObject
{
    [Header("Ground")]
    public StateAnimSet Idle;
    public StateAnimSet Walk;
    public StateAnimSet Sprint;
    public StateAnimSet Dash;
    public StateAnimSet Slide;

    [Header("Airborne")]
    public StateAnimSet Jump;
    public StateAnimSet Fall;

    [Header("Wall")]
    public StateAnimSet WallRunLeft;
    public StateAnimSet WallRunRight;
    public StateAnimSet WallKick;

    [Header("Ledge")]
    public StateAnimSet LedgeGrab;
    public StateAnimSet LedgeClimb;
}
