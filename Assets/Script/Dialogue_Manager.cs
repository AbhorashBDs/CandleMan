using UnityEngine;
using Ink.Runtime;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Dialogue_Manager : MonoBehaviour
{
    //Singleton pattern
    public static Dialogue_Manager instance;
    public static Dialogue_Manager Instance { get; private set; }

    //Ink assets
    public TextAsset _inkAsset;
    Story _inkStory;

    //UI
    public GameObject _panelDialogue;
    public GameObject _panelChoice;
    public TMPro.TextMeshProUGUI _textDialogue;
    public Button[] _choices;

    //Input
    public InputActionReference _clic;

    void Awake()
    {
        //Si on a des problèmes, c'est peut-être qu'il faut détruire "this" et pas "gameobject".
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        _inkStory = new Story(_inkAsset.text);
        _panelChoice.SetActive(false);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ContinueDialogue(_inkStory);
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void ContinueDialogue(Story inkStory)
    {
        if (inkStory.canContinue)
        {
            inkStory.Continue();
            _textDialogue.text = inkStory.currentText;
        }
        else if (inkStory.currentChoices.Count > 0)
        {
            _panelDialogue.SetActive(false);
            _panelChoice.SetActive(true);
            for (int i = 0; i < inkStory.currentChoices.Count; i++)
            {
                _choices[i].GetComponentInChildren<TextMeshProUGUI>().text = inkStory.currentChoices[i].text;
            }
        }
    }

    private void OnEnable()
    {
        _clic.action.started += OnClic;
    }

    private void OnDisable()
    {
        _clic.action.started -= OnClic;
    }

    private void OnClic(InputAction.CallbackContext context)
    {
        Debug.Log("Does clic work ?");
        ContinueDialogue(_inkStory);
    }
}
