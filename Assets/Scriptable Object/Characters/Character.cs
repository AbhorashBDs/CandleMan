using UnityEngine;

[CreateAssetMenu(fileName = "Character", menuName = "Scriptable Objects/Character")]
public class Character : ScriptableObject
{
    public TextAsset _inkText;
    public string _name;
    public Sprite _portrait;
}
