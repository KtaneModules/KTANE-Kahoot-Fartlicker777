using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class Kahoot : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;

    private KMAudio.KMAudioRef SoundIThink;

    public KMSelectable BigIfTrue;
    public KMSelectable EnterButton;

    public Renderer Background;

    public TextMesh EnterContinue;
    public TextMesh GamePIN;
    public TextMesh TheQuestion;

    public Material[] ColorsForButtons;
    public SpriteRenderer[] SymbolsForButtons;
    public Sprite[] SymbolsForButtonsButSprites;
    public KMSelectable[] AnswerChoicesButtons;

    public GameObject StageOneShit;
    public GameObject LoadingShit;
    public GameObject StageTwoShit;
    public GameObject ContinueButton;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    int[] Shuffler = {0, 1, 2, 3};
    List<int> PreviousQuestions = new List<int> {};
    int Answer;
    int Batteries;
    int Goal;
    int Minutes;
    int Modules;
    int Ports;
    int QuestionNumber = 0;
    int SerialNumberLast;
    int SerialNumberLetters;
    int Stage;

    private KeyCode[] TheKeys = {
        KeyCode.Backspace, KeyCode.Return, KeyCode.Alpha1, KeyCode.Keypad1, KeyCode.Alpha2, KeyCode.Keypad2, KeyCode.Alpha3, KeyCode.Keypad3,
        KeyCode.Alpha4, KeyCode.Keypad4, KeyCode.Alpha5, KeyCode.Keypad5, KeyCode.Alpha6, KeyCode.Keypad6, KeyCode.Alpha7, KeyCode.Keypad7,
        KeyCode.Alpha8, KeyCode.Keypad8, KeyCode.Alpha9, KeyCode.Keypad9, KeyCode.Alpha0, KeyCode.Keypad0
    };

    string[] ColorsForLog = {"red", "blue", "yellow", "green"};
    string[] PossibleCorrects = {"Genius machine?", "Room A9\nPerfections", "Lightning Smart?", "Pure Genius?"};
    string[] Questions = {
      "How many batteries\nare on the bomb?",
      "How many ports\nare on the bomb?",
      "The amount of minutes\non the bomb originally\nwere...?",
      "There are more\n___ indicators.",
      "The amount of solvable\nmodules on the bomb\nare...?",
      "The last digit of the\nserial number is?",
      "There are _ letters in\nthe serial number."
    };
    string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ***0123456789";
    string Code = String.Empty;
    string InputCommand = String.Empty;
    string TheLetters = "BE11223344556677889900";

    float Hue = 0f;
    float Saturation = .45f;
    float Value = 1f;

    bool Activated;
    bool Focused;
    bool Highlighted;
    bool? Indicators = null;

    void Awake () {
        moduleId = moduleIdCounter++;
        BigIfTrue.OnHighlight += delegate () { MusicStarter(); Highlighted = true;};
        BigIfTrue.OnHighlightEnded += delegate () { MusicEnder(); Highlighted = false;};
        BigIfTrue.OnFocus += delegate () { Focused = true; };
        BigIfTrue.OnDefocus += delegate () { MusicEnder(); Focused = false;};
        StageTwoShit.gameObject.SetActive(false);
        foreach (KMSelectable Button in AnswerChoicesButtons) {
          Button.OnInteract += delegate () { ButtonPress(Button); return false; };
        }
        EnterButton.OnInteract += delegate () { EnterPress(); return false; };
        if (Application.isEditor) {
            Focused = true;
        }
    }

    void Start () {
      Hue = UnityEngine.Random.Range(0, 1f);
      StartCoroutine(BackgroundColorChanger());
      Modules = Bomb.GetSolvableModuleNames().Count();
      Minutes = (int) (Bomb.GetTime() / 60);
      Batteries = Bomb.GetBatteryCount();
      Ports = Bomb.GetPortCount();
      if (Bomb.GetOnIndicators().Count() > Bomb.GetOffIndicators().Count())
        Indicators = true;
      else if (Bomb.GetOnIndicators().Count() < Bomb.GetOffIndicators().Count())
        Indicators = false;
      SerialNumberLast = Bomb.GetSerialNumberNumbers().Last();
      SerialNumberLetters = Bomb.GetSerialNumberLetters().Count();
      Goal = UnityEngine.Random.Range(3, 6);
      for (int i = 0; i < 6; i++)
        Code += (Alphabet.IndexOf(Bomb.GetSerialNumber()[i]) + 1) % 10;
      Debug.LogFormat("[Kahoot #{0}] The code for the Kahoot! game is {1}.", moduleId, Code);
    }

    void ButtonPress (KMSelectable Button) {
      LoadingShit.gameObject.SetActive(true);
      ContinueButton.gameObject.SetActive(true);
      for (int i = 0; i < 3; i++) {
        if (Button == AnswerChoicesButtons[i]) {
          if (Answer == Shuffler[i]) {
            Stage++;
            if (Stage == Goal) {
              GetComponent<KMBombModule>().HandlePass();
              EnterContinue.text = "Good job!";
              TheQuestion.text = "Secret defusing\npowers?";
              StageTwoShit.gameObject.SetActive(false);
              LoadingShit.gameObject.SetActive(true);
              moduleSolved = true;
              return;
            }
            StageTwoShit.gameObject.SetActive(false);
            StopAllCoroutines();
            StartCoroutine(BackgroundColorChanger());
            TheQuestion.text = PossibleCorrects[UnityEngine.Random.Range(0, PossibleCorrects.Length)];
          }
          else {
            TheQuestion.text = "Were you tooooooo\nfast?";
            GetComponent<KMBombModule>().HandleStrike();
            StageTwoShit.gameObject.SetActive(false);
            StopAllCoroutines();
            StartCoroutine(BackgroundColorChanger());
            Stage = 0;
          }
        }
      }
    }

    void MusicStarter () {
      if (SoundIThink == null)
        SoundIThink = Audio.PlaySoundAtTransformWithRef("Kahoot!", transform);
    }

    void MusicEnder () {
      if (SoundIThink != null && !Focused) {
        SoundIThink.StopSound();
        SoundIThink = null;
      }
    }

    void EnterPress () {
      if (moduleSolved)
        return;
      if (EnterContinue.text == "Enter") {
        if (InputCommand == Code) {
          Activated = true;
          StartCoroutine(QuestionChooser());
        }
        else {
          GetComponent<KMBombModule>().HandleStrike();
          InputCommand = String.Empty;
        }
      }
      else
        StartCoroutine(QuestionChooser());
    }

    IEnumerator BackgroundColorChanger () {
      while (true) {
        for (int i = 0; i < 256; i++) {
          Hue += 0.0027f;
          Hue %= 1f;
          Background.material.color = Color.HSVToRGB(Hue, Saturation, Value);
          yield return new WaitForSeconds(0.025f);
        }
      }
    }

    void Update () {
      if (!Focused && !Highlighted) {
        SoundIThink.StopSound();
        SoundIThink = null;
      }
      for (int i = 0; i < TheKeys.Count(); i++) {
          if (Input.GetKeyDown(TheKeys[i]) && Focused && !Activated) {
            if (InputCommand.Length == 6 && TheLetters[i].ToString() != "B".ToString() && TheLetters[i].ToString() != "E".ToString())
              return;
            if (TheLetters[i].ToString() == "B".ToString()) {
              if (InputCommand == String.Empty)
                return;
              InputCommand = InputCommand.Substring(0, InputCommand.Length - 1);
              GamePIN.text = InputCommand.ToUpper();
            }
            else if (TheLetters[i].ToString() == "E".ToString())
              EnterButton.OnInteract();
            else
              HandleKey(TheLetters[i]);
          }
        }
        if (InputCommand == String.Empty) {
          GamePIN.color = new Color32(118, 118, 118, 255);
          GamePIN.text = "Game PIN";
        }
        else
          GamePIN.color = new Color32(0, 0, 0, 255);
    }

    void HandleKey (char c) {
      if (Focused) {
        InputCommand += c.ToString();
        GamePIN.text = InputCommand.ToUpper();
      }
    }

    IEnumerator QuestionChooser () {
      EnterContinue.text = "Continue";
      ContinueButton.gameObject.SetActive(false);
      StageOneShit.gameObject.SetActive(false);
      LoadingShit.gameObject.SetActive(true);
      StageTwoShit.gameObject.SetActive(false);
      QuestionNumber = UnityEngine.Random.Range(0, Questions.Length);
      for (int i = 0; i < PreviousQuestions.Count(); i++)
        while (QuestionNumber == PreviousQuestions[i])
          QuestionNumber = UnityEngine.Random.Range(0, Questions.Length);
      PreviousQuestions.Add(QuestionNumber);
      TheQuestion.text = Questions[QuestionNumber];
      Shuffler.Shuffle();
      Debug.LogFormat("[Kahoot #{0}] The question is \"{1}\" and the missing color is {2}.", moduleId, TheQuestion.text.Replace('\n', ' '), ColorsForLog[Shuffler[3]]);
      yield return new WaitForSecondsRealtime(5f);
      StageTwoShit.gameObject.SetActive(true);
      LoadingShit.gameObject.SetActive(false);
      for (int i = 0; i < 3; i++) {
        AnswerChoicesButtons[i].GetComponent<MeshRenderer>().material = ColorsForButtons[Shuffler[i]];
        SymbolsForButtons[i].GetComponent<SpriteRenderer>().sprite = SymbolsForButtonsButSprites[Shuffler[i]];
      }
      switch (QuestionNumber) {//RBYG
        case 0: //bat
        switch (Shuffler[3]) {
          case 0:
          switch (Batteries) {
            case 0: case 1: case 2: Answer = 1; break;
            case 3: case 4: Answer = 2; break;
            default: Answer = 3; break;
          }
          break;
          case 1:
          switch (Batteries) {
            case 0: case 1: case 2: Answer = 0; break;
            case 3: case 4: Answer = 3; break;
            default: Answer = 2; break;
          }
          break;
          case 2:
          switch (Batteries) {
            case 0: case 1: case 2: Answer = 3; break;
            case 3: case 4: Answer = 1; break;
            default: Answer = 0; break;
          }
          break;
          case 3:
          switch (Batteries) {
            case 0: case 1: case 2: Answer = 2; break;
            case 3: case 4: Answer = 0; break;
            default: Answer = 1; break;
          }
          break;
        }
        break;
        case 1: //port RBYG
        switch (Shuffler[3]) {
          case 0:
          switch (Ports) {
            case 0: case 1: case 2: Answer = 1; break;
            case 3: case 4: case 5: case 6: Answer = 2; break;
            default: Answer = 3; break;
          }
          break;
          case 1:
          switch (Ports) {
            case 0: case 1: case 2: Answer = 0; break;
            case 3: case 4: case 5: case 6: Answer = 3; break;
            default: Answer = 2; break;
          }
          break;
          case 2:
          switch (Ports) {
            case 0: case 1: case 2: Answer = 3; break;
            case 3: case 4: case 5: case 6: Answer = 1; break;
            default: Answer = 0; break;
          }
          break;
          case 3:
          switch (Ports) {
            case 0: case 1: case 2: Answer = 2; break;
            case 3: case 4: case 5: case 6: Answer = 0; break;
            default: Answer = 1; break;
          }
          break;
        }
        break;
        case 2: //min RBYG
        switch (Shuffler[3]) {
          case 0:
          switch (SquareAndPrimeChecker(Minutes)) {
            case false: Answer = 1; break;
            case true: Answer = 2; break;
            default: Answer = 3; break;
          }
          break;
          case 1:
          switch (SquareAndPrimeChecker(Minutes)) {
            case false: Answer = 0; break;
            case true: Answer = 3; break;
            default: Answer = 2; break;
          }
          break;
          case 2:
          switch (SquareAndPrimeChecker(Minutes)) {
            case false: Answer = 3; break;
            case true: Answer = 1; break;
            default: Answer = 0; break;
          }
          break;
          case 3:
          switch (SquareAndPrimeChecker(Minutes)) {
            case false: Answer = 2; break;
            case true: Answer = 0; break;
            default: Answer = 1; break;
          }
          break;
        }
        break;
        case 3: //ind RBYG
        switch (Shuffler[3]) {
          case 0:
          switch (Indicators) {
            case true: Answer = 1; break;
            case false: Answer = 2; break;
            default: Answer = 3; break;
          }
          break;
          case 1:
          switch (Indicators) {
            case true: Answer = 0; break;
            case false: Answer = 3; break;
            default: Answer = 2; break;
          }
          break;
          case 2:
          switch (Indicators) {
            case true: Answer = 3; break;
            case false: Answer = 1; break;
            default: Answer = 0; break;
          }
          break;
          case 3:
          switch (Indicators) {
            case true: Answer = 2; break;
            case false: Answer = 0; break;
            default: Answer = 1; break;
          }
          break;
        }
        break;
        case 4: //mod RBYG
        switch (Shuffler[3]) {
          case 0:
          switch (DivisibilityChecker(Modules)) {
            case true: Answer = 1; break;
            case false: Answer = 2; break;
            default: Answer = 3; break;
          }
          break;
          case 1:
          switch (DivisibilityChecker(Modules)) {
            case true: Answer = 0; break;
            case false: Answer = 3; break;
            default: Answer = 2; break;
          }
          break;
          case 2:
          switch (DivisibilityChecker(Modules)) {
            case true: Answer = 3; break;
            case false: Answer = 1; break;
            default: Answer = 0; break;
          }
          break;
          case 3:
          switch (DivisibilityChecker(Modules)) {
            case true: Answer = 2; break;
            case false: Answer = 0; break;
            default: Answer = 1; break;
          }
          break;
        }
        break;
        case 5: //sn# RBYG
        switch (Shuffler[3]) {
          case 0:
          switch (SerialNumberLast) {
            case 0: case 1: case 2: case 3: Answer = 1; break;
            case 4: case 5: case 6: Answer = 2; break;
            default: Answer = 3; break;
          }
          break;
          case 1:
          switch (SerialNumberLast) {
            case 0: case 1: case 2: case 3: Answer = 0; break;
            case 4: case 5: case 6: Answer = 3; break;
            default: Answer = 2; break;
          }
          break;
          case 2:
          switch (SerialNumberLast) {
            case 0: case 1: case 2: case 3: Answer = 3; break;
            case 4: case 5: case 6: Answer = 1; break;
            default: Answer = 0; break;
          }
          break;
          case 3:
          switch (SerialNumberLast) {
            case 0: case 1: case 2: case 3: Answer = 2; break;
            case 4: case 5: case 6: Answer = 0; break;
            default: Answer = 1; break;
          }
          break;
        }
        break;
        case 6: //snx RBYG
        switch (Shuffler[3]) {
          case 0:
          switch (SerialNumberLetters) {
            case 0: case 1: case 2: Answer = 1; break;
            case 3: case 4: Answer = 2; break;
            default: Answer = 3; break;
          }
          break;
          case 1:
          switch (SerialNumberLetters) {
            case 0: case 1: case 2: Answer = 0; break;
            case 3: case 4: Answer = 3; break;
            default: Answer = 2; break;
          }
          break;
          case 2:
          switch (SerialNumberLetters) {
            case 0: case 1: case 2: Answer = 3; break;
            case 3: case 4: Answer = 1; break;
            default: Answer = 0; break;
          }
          break;
          case 3:
          switch (SerialNumberLetters) {
            case 2: Answer = 2; break;
            case 3: Answer = 0; break;
            default: Answer = 1; break;
          }
          break;
        }
        break;
      }
      for (int i = 0; i < 3; i++)
        if (Shuffler[i] == Answer)
          Debug.LogFormat("[Kahoot #{0}] The answer is {1}.", moduleId, ColorsForLog[Shuffler[i]]);
      yield return new WaitForSecondsRealtime(10f);
      GetComponent<KMBombModule>().HandleStrike();
    }

    bool? SquareAndPrimeChecker (int M) {
      if (M == 1) return false;
      if (M == 2) return true;
      if (M % 2 == 0) goto SquareChecker;

      var boundary = (int)Math.Floor(Math.Sqrt(M));

      for (int i = 3; i <= boundary; i += 2)
          if (M % i == 0)
              goto SquareChecker;

      return true;

      SquareChecker:
      double SquareTime = M;
      if (Math.Sqrt(SquareTime) % 1 == 0)
        return false;
      else
        return null;
    }

    bool? DivisibilityChecker (int M) {
      if (M % 3 == 0 && M % 2 != 0)
        return true;
      else if (M % 3 != 0 && M % 2 == 0)
        return false;
      else
        return null;
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} type ###### to enter in the room code. Use !{0} TL/BL/BR to press that button. Use !{0} continue to continue.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string Command) {
      Command = Command.Trim().ToUpper();
      string AlphabetTwoElectricBoogaloo = "QWERTYUIOPASDFGHJKLZXCVBNM";
      if (Command == "CONTINUE")
        EnterButton.OnInteract();
      else if (Command == "TL")
        AnswerChoicesButtons[0].OnInteract();
      else if (Command == "BL")
        AnswerChoicesButtons[1].OnInteract();
      else if (Command == "BR")
        AnswerChoicesButtons[2].OnInteract();
      else {
        string[] Parameters = Command.Split(' ');
        if (Parameters[0] != "TYPE" || Parameters.Length != 2)
          goto GoToJail;
        if (Parameters[1].Length != 6 || Parameters[1].Any(x => AlphabetTwoElectricBoogaloo.Contains(x)))
          goto GoToJail;
        for (int i = 0; i < 6; i++) {
          HandleKey(Parameters[1][i]);
          yield return new WaitForSecondsRealtime(.1f);
        }
        EnterButton.OnInteract();
        if (true)
          yield break;
        GoToJail:
        yield return "sendtochaterror I don't understand!";
      }
    }

    IEnumerator TwitchHandleForcedSolve () {
      while (!moduleSolved) {
        if (!Activated) {
          for (int i = 0; i < 6; i++) {
            HandleKey(Code[i]);
            yield return new WaitForSecondsRealtime(.1f);
          }
          EnterButton.OnInteract();
        }
        if (StageTwoShit.gameObject.activeSelf) {
          for (int i = 0; i < 3; i++)
            if (Answer == Shuffler[i])
              AnswerChoicesButtons[i].OnInteract();
          yield return new WaitForSecondsRealtime(.1f);
          EnterButton.OnInteract();
        }
        else
          yield return new WaitForSeconds(1f);
      }
    }
}
