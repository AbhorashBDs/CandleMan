using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ink.Runtime;
using TMPro;
using UnityEngine.UI;

public class Dialogue_Manager : MonoBehaviour
{
    //Singleton pattern
    private static Dialogue_Manager instance;
    public static Dialogue_Manager Instance { get; private set; }

    //Ink assets -> On va virer l'inkAsset à terme car il viendra d'autres parts.
    [SerializeField] private TextAsset _inkAsset;
    [HideInInspector] public Story _currentInkStory;
    //boolean pour savoir si la coroutine du dialogue est en cours
    public bool _dialogueIsPlaying { get; private set; }

    [Header("Dialogue UI")]
    //UI
    [SerializeField] private GameObject _panelDialogue;
    [SerializeField] private GameObject _panelChoice;
    //Le parent qui contient tous les boutons pour pouvoir en ajouter sans problème.
    [SerializeField] private GameObject _choicesParent;
    private List<Button> _choices;
    [SerializeField] private TextMeshProUGUI _textDialogue, _textName;
    private Sprite currentImage;
    [SerializeField] private Image _portraitLeft, _portraitRight;
    [SerializeField] private Image _continueIcon;
    [SerializeField] private GameObject _scrollBar;
    //L'idée c'est de récupérer la sensitivité de la scrollBar pour pouvoir l'enlever et la remettre manuellement.
    private float _scrollSensitivity;

    [Header("Characters")]
    //Personnage
    public Character[] _characters;

    //Paramètres de dialogues
    [Header("DialogueParameters")]
    [SerializeField] private float _typingSpeed;
    private Coroutine _displayLineCoroutine;
    [HideInInspector] public bool _canContinueToNextLine =true;

    //Tags
    private const string SPEAKER_TAG = "speaker";
    private const string LAYOUT_TAG = "layout";

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
        //SetUp de sensitivité
        _scrollSensitivity = _panelChoice.GetComponent<ScrollRect>().scrollSensitivity;
        //Set up des boutons pour récupérer le nombre de bouton si on veut en rajouter.
        SetButtons();
        //Setup des boutons et des panels de choix.
        DisactivateButton();
        //On désactive toute la page de dialogue de base pour qu'elle soit initialiser proprement dans les fonctions suivantes
        _panelDialogue.SetActive(false);
        _dialogueIsPlaying = false;
        _portraitLeft.gameObject.SetActive(false);
        _portraitRight.gameObject.SetActive(false);

        //if (savedStory!=null)
        //_currentInkStory.state.LoadJson(savedStory);

        //Ligne à mettre quand on lance un dialogue depuis un autre endroit, à enlever à terme.
        EnterDialogueMode(_inkAsset);
    }

    // Update is called once per frame
    void Update()
    {
    }


    #region Dialogue
    //Fonction que l'on lance pour continuer l'histoire quand on est en dialogue.
    public void ContinueDialogue(Story inkStory)
    {
        if (inkStory.canContinue)
        {
            //Sécurité pour ne pas lancer 2 fois la coroutine
            if(_displayLineCoroutine != null)
            {
                StopCoroutine(_displayLineCoroutine);
            }
            //Coroutine pour afficher les lettres 1 par 1
            _displayLineCoroutine = StartCoroutine(DisplayLine(inkStory.Continue()));
            //Fonction qui gère les différents tags et qui set up l'UI en fonction
            HandleTags(inkStory.currentTags);
            //savedStory = _inkStory.state.ToJson();
        }
        else if (inkStory.currentChoices.Count > 0)
        {
            //Fonction pour lancer les choix
            DisplayChoices(inkStory);
        }
        else
        {
            //On lance une coroutine plutôt qu'une fonction pour qu'il y ait un petit délai à la fin du dialogue
            StartCoroutine(ExitDialogueMode());
        }
    }

    //Affiche les caractères 1 par 1
    private IEnumerator DisplayLine(string line)
    {
        //récupère la ligne de dialogue
        _textDialogue.text = line;
        _textDialogue.maxVisibleCharacters = 0;

        _canContinueToNextLine = false;
        _continueIcon.enabled = false;

        bool isAddingRichTextTag = false;

        foreach(char letter in line.ToCharArray())
        {
            //On regarde si le joueur a appuyé une seconde fois pour faire apparaître la fin du dialogue
            if (Input_Manager.Instance._endCoroutine)
            {
                //On affiche alors la fin du dialogue
                _textDialogue.maxVisibleCharacters = line.Length;
                _continueIcon.enabled=true;
                Input_Manager.Instance._endCoroutine = false;
                break;
            }

            //On regarde s'il y a un chevron qui indique une balise HTML, s'il y en a une on continue la boucle mais on n'autorise pas d'afficher plus de caractères visibles donc ça ne s'affiche pas à l'écran
            if(letter == '<' || isAddingRichTextTag)
            {
                isAddingRichTextTag = true;
                if (letter == '>')
                {
                    isAddingRichTextTag = false;
                }
            }
            else
            {
                _textDialogue.maxVisibleCharacters++;
                yield return new WaitForSeconds(_typingSpeed);
            }

                
        }

        _canContinueToNextLine = true;
        _continueIcon.enabled = true;
    }

    //Fonction que les boutons récupèrent pour déterminer quel choix a été sélectionner par le joueur. -> Elle est référencé dans chaque bouton
    public void ChoiceSelection(Button button)
    {
        for(int i = 0; i < _choices.Count; i++)
        {
            //On récupère le choix correspondant au bouton et on relance le dialogue correspondant
            if (_choices[i] == button)
            {
                _currentInkStory.ChooseChoiceIndex(i);
                HandleTags(_currentInkStory.currentTags);
                ContinueDialogue(_currentInkStory);
                DisactivateButton();
                _panelDialogue.SetActive(true);
                return;
            }
        }
    }

    //Fonction pour gérer les tags dans ink
    private void HandleTags(List<string> currentTags)
    {
        //Loop through each tag and handle it accordingly
        foreach (string tag in currentTags)
        {
            //On sépare les tags pour récupérer les noms et les données des tags avec une petite gestion d'erreur
            string[] splitTag = tag.Split(":");
            if(splitTag.Length != 2)
            {
                Debug.LogError("Tag could not be appropriately parsed: " + tag);
            }
            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();

            //handle tag
            switch(tagKey)
            {
                //Quand c'est le nom de la personne qui parle on change le portrait en fonction du personnage.
                case SPEAKER_TAG:
                    foreach(Character chara in _characters)
                    {
                        if(chara.name==tagValue)
                        {
                            _textName.text = tagValue;
                            currentImage = chara._portrait;
                        }
                    }
                    break;
                //On affiche le portrait au bon emplacement selon l'indication du tag
                case LAYOUT_TAG:
                    if(currentImage!=null)
                    {
                        _portraitLeft.gameObject.SetActive(false);
                        _portraitRight.gameObject.SetActive(false);
                        if (tagValue=="left")
                        {
                            _portraitLeft.gameObject.SetActive(true);
                            _portraitLeft.sprite = currentImage;
                        }
                        else if(tagValue=="right")
                        {
                            _portraitRight.gameObject.SetActive(true);
                            _portraitRight.sprite = currentImage;
                        }
                    }
                    break;
                default:
                    Debug.LogWarning("Tag came in but is not currently being handled: " + tag);
                    break;
            }
        }
    }

    //Fonction pour lancer un dialogue depuis une autre phase de gameplay
    public void EnterDialogueMode(TextAsset inkJSON)
    {
        //Initialisation de l'histoire + gestion des erreurs d'ink.
        _currentInkStory = new Story(_inkAsset.text);
        _currentInkStory.onError += (msg, type) => {
            if (type == Ink.ErrorType.Warning)
                Debug.LogWarning(msg);
            else
                Debug.LogError(msg);
        };

        _panelDialogue.SetActive(true);
        _dialogueIsPlaying = true;

        ContinueDialogue(_currentInkStory);
    }

    //On sort du dialogue
    private IEnumerator ExitDialogueMode()
    {
        yield return new WaitForSeconds(0.2f);

        _dialogueIsPlaying=false;
        _panelDialogue.SetActive(false);
        DisactivateButton();
        _portraitLeft.gameObject.SetActive(false);
        _portraitRight.gameObject.SetActive(false);
        _textDialogue.text = "";
    }

    #endregion

    #region UI
    //Fonction pour désactiver chaque bouton manuellement
    private void DisactivateButton()
    {
        foreach (Button button in _choices)
        {
            button.gameObject.SetActive(false);
        }
        _panelChoice.SetActive(false);
    }

    //Fonction qui récupère tous les boutons dans l'UI d'unity pour ne pas le faire à la main
    private void SetButtons()
    {
        _choices=new List<Button>();
        foreach(Button button in _choicesParent.GetComponentsInChildren<Button>())
        {
            _choices.Add(button);           
        }
    }

    //Fonction qui affiche les choix
    private void DisplayChoices(Story inkStory)
    {
        _panelDialogue.SetActive(false);
        _panelChoice.SetActive(true);
        //Fonction pour savoir s'il faut afficher le scrolling ou non
        if (inkStory.currentChoices.Count >= 5)
        {
            SwitchScroll(true);
        }
        else SwitchScroll(false);
        //Fonction qui affiche chaque choix en ne l'affichant que si le nombre de choix de l'histoire correspond
        for (int i = 0; i < inkStory.currentChoices.Count; i++)
        {
            _choices[i].gameObject.SetActive(true);
            _choices[i].GetComponentInChildren<TextMeshProUGUI>().text = inkStory.currentChoices[i].text;
        }
    }

    //Fonction qui permet ou non le scrolling selon si la condition le permet
    private void SwitchScroll(bool conidtion)
    {
        //Met la vitesse à 0 et la réactive 
        if (!conidtion)
        {
            _panelChoice.GetComponent<ScrollRect>().scrollSensitivity = 0;
        }
        else
        {
            _panelChoice.GetComponent<ScrollRect>().scrollSensitivity = _scrollSensitivity;
        }

    }

    #endregion

}
