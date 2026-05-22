using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Ink.Runtime;

public class Input_Manager : MonoBehaviour
{
    //Singleton pattern
    private static Input_Manager instance;
    public static Input_Manager Instance { get; private set; }

    //Input
    public InputActionReference _clic;

    public bool _endCoroutine = false;


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
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    #region inputManager
    //Fonction nécessaire à l'input manager.
    private void OnEnable()
    {
        _clic.action.started += OnClic;
    }

    private void OnDisable()
    {
        _clic.action.started -= OnClic;
    }

    //Fonction qui se lance quand le joueur clic sur l'écran
    public void OnClic(InputAction.CallbackContext context)
    {
        Story currentStory = Dialogue_Manager.Instance._currentInkStory;

        //Continue le dialogue si le dialogue ne se lance pas de suite, sinon change le boolean pour que le clic finisse le dialogue
        if (currentStory != null)
        {
            if(Dialogue_Manager.Instance._canContinueToNextLine)
            {
                Dialogue_Manager.Instance.ContinueDialogue(currentStory);
            }
            else
            {
                _endCoroutine = true;
            }
            
        }
        else
        {
            Debug.LogError("There isn't any story or the current story is still typed");
        }
    }
    #endregion

}