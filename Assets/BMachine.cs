using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class BMachine : MonoBehaviour
{
    public KMSelectable[] buttons;
    public TextMesh command;
    StringBuilder tape;
    int pointer;
    int currentCommand;
    string[] program;
    int startingTime;
    int inputCounter;
    bool recovering;
    public KMBombModule module;
    public KMAudio sound;
    public KMBombInfo bomb;
    int moduleId;
    static int moduleIdCounter = 1;
    // Use this for initialization
    void Awake()
    {
        moduleId = moduleIdCounter++;
        buttons[0].OnInteract += delegate { PressButton(buttons[0], ' '); return false; };
        buttons[1].OnInteract += delegate { PressButton(buttons[1], '*'); return false; };
        module.OnActivate += delegate { Activate(); };
    }

    void Activate()
    {
        startingTime = (int)bomb.GetTime() / 60;
        if (startingTime == 0) startingTime = 10;
        program = new string[startingTime];
        tape = new StringBuilder(" ", 5 * startingTime);
        GenerateProgram();
        ExecuteProgram();
        StartCoroutine(ShowProgram());
    }

    void GenerateProgram()
    {
        int i = 0;
        while (i < startingTime)
        {
            int loopsize;
            int beforeloop;
            beforeloop = UnityEngine.Random.Range(1, 6);
            loopsize = UnityEngine.Random.Range(2, 7);
            if (beforeloop + loopsize + i >= startingTime)
            {
                break;
            };
            program[i + beforeloop + loopsize] = string.Format("C{0}", i + beforeloop);
            i += beforeloop + loopsize + 1;
        }
        for (int j = 0; j < startingTime; j++)
        {

            List<string> symbols = new List<string> { "→", "←", "*"};


            if (program[j] != null)
            {

                continue;
            }
            if (j != program.Count() - 1)
            {
                if (program[j + 1] != null)
                {

                    symbols.Remove("*");
                }
            }
            if (j != 0)
            {
                
                if (program[j - 1] == "*" )
                {
                    symbols.Remove("*");
                }

                if (program[j - 1] == "→")
                {
                    symbols.Remove("←");
                }

                if (program[j - 1] == "←")
                {
                    symbols.Remove("→");
                }
            }

            //Gives a valid symbol to the program
            program[j] = symbols[UnityEngine.Random.Range(0, symbols.Count)];
        }
        Debug.LogFormat("[B-Machine #{0}] Full program: {1}.", moduleId, program.Join(","));
    }
    void ExecuteProgram()
    {
        int loopstart = 0;

        //Int that stores how many time the loop has looped
        int looptimes = 0;
        for (int i = 0; i < program.Count(); i++)
        {
            if (program[i].Equals("←"))
            {
                if (pointer == 0)
                {
                    tape.Insert(0, " ");
                    Debug.LogFormat("[B-Machine #{0}] Going past position 0, adding another cell to the beginning of the tape.", moduleId);
                }
                else
                {
                    pointer -= 1;
                    Debug.LogFormat("[B-Machine #{0}] Going left, now in position {1}.", moduleId, pointer);
                }
            }
            else if (program[i].Equals("→"))
            {
                pointer++;
                if (pointer == tape.Length)
                {
                    tape.Append(" ");
                }
                Debug.LogFormat("[B-Machine #{0}] Going right, now in position {1}.", moduleId, pointer);
            }
            else if (program[i].Equals("*"))
            {
                tape[pointer] = '*';
                Debug.LogFormat("[B-Machine #{0}] Marking position {1}.", moduleId, pointer);
            }
            else if (program[i][0] == 'C')
            {
                loopstart =  int.Parse(program[i].Remove(0, 1)) - 2;
                if ((tape[pointer] == ' ') | (looptimes == 5))
                {
                    Debug.LogFormat("[B-Machine #{0}] End of loop.", moduleId);
                    looptimes = 0;
                }
                else
                {
                    looptimes++;
                    i = loopstart;
                    Debug.LogFormat("[B-Machine #{0}] Going to the start of the loop. You have now looped {1} time(s).", moduleId, looptimes);
                }
            }
        }
        Debug.LogFormat("[B-Machine #{0}] End state of tape:\n {1}", moduleId, tape);
        for (int i = 0; i < program.Count(); i++) program[i] = string.Format("{0}. {1}", i + 1, program[i]);
    }
    void PressButton(KMSelectable button, char input)
    {
        button.AddInteractionPunch();
        sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        StartCoroutine(AnimKeys(button));
        if (command.text == "READY")
        {          
            if (tape[inputCounter] == input)
            {
                inputCounter++;
                if (inputCounter == tape.Length)
                {
                    sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                    module.HandlePass();
                    command.text = "SOLVED";
                }
            }
            else
            {
                module.HandleStrike();
                inputCounter = 0;
                command.text = "RECOVER?";        
            }
        }
        else if (command.text == "RECOVER?")
        {
            if (input == '*')
            {
                recovering = true;
                command.text = program[inputCounter];
            }
            else if (input == ' ') command.text = "READY";
        }
        else if (recovering)
        {
           if (input == '*')
            {
                inputCounter++;
                if (inputCounter == program.Length)
                {
                    inputCounter = 0;
                    command.text = "READY";
                    recovering = false;
                }
                else command.text = program[inputCounter];
            }
            else if (input == ' ')
            {
                inputCounter--;
                if (inputCounter == -1)
                {
                    inputCounter = 0;
                    command.text = "READY";
                    recovering = false;
                }
                command.text = program[inputCounter];
            }
        }
           
    }
    IEnumerator ShowProgram()
    {
        int tick = 0;
        while (tick < startingTime)
        {
            command.text = program[tick];
            sound.PlaySoundAtTransform("More Cowbell", transform);
            tick++;
            yield return new WaitForSeconds(30f);
        }
        command.text = "READY";
        yield break;
    }
   IEnumerator AnimKeys(KMSelectable button)
    {
        {
            for (int i = 0; i < 3; i++)
            {
                button.transform.localPosition += new Vector3(0f, -0.001f, 0f);
                yield return new WaitForSeconds(0.02f);
            }
            for (int i = 0; i < 3; i++)
            {
                button.transform.localPosition += new Vector3(0f, 0.001f, 0f);
                yield return new WaitForSeconds(0.02f);
            }
        }
    }
#pragma warning disable 414
    private string TwitchHelpMessage = "Use e.g. '!{0} submit 110110' to submit the tape as binary with 1 = marked and 0 = unmarked. Stage Recovery: Use '!{0} cycle' to cycle through the stages in order. Use '!{0} show <number>' to go directly to that stage. Use '!{0} abort' to end the stage recovery.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant().Trim();
        string validcmds = "01";
        string[] commandArray = command.Split(' ');
        if (this.command.text == "RECOVER?")
        {
            if (command == "cycle")
            {
                yield return null;
                buttons[1].OnInteract();
                while (recovering)
                {
                    yield return new WaitForSeconds(2f);
                    buttons[1].OnInteract();
                }
                yield break;
            }
            if (command == "abort")
            {
                yield return null;
                buttons[0].OnInteract();
                yield break;
            }
            int stage;
            if (commandArray.Length == 2 && commandArray[0] == "show" && int.TryParse(commandArray[1], out stage) && stage > 0 && stage <= program.Length)
            {
                yield return null;
                buttons[1].OnInteract();
                while (inputCounter < stage - 1)
                {
                    yield return new WaitForSeconds(0.2f);
                    buttons[1].OnInteract();
                }
                yield break;
            }
            else
            {
                yield return "sendtochaterror @{0}, invalid command.";
                yield break;
            }
        }
        if (recovering)
        {
            if (command == "abort")
            {
                yield return null;
                while (recovering)
                {
                    yield return new WaitForSeconds(0.2f);
                    buttons[1].OnInteract();
                }
                yield break;
            }
            int stage;
            if (commandArray.Length == 2 && commandArray[0] == "show" && int.TryParse(commandArray[1], out stage) && stage > 0 && stage <= program.Length)
            {
                yield return null;
                KMSelectable direction = inputCounter < stage ? buttons[1] : buttons[0];
                while (inputCounter != stage - 1)
                {
                    yield return new WaitForSeconds(0.2f);
                    direction.OnInteract();
                }
                yield break;
            }
        }
        if (commandArray.Length != 2 || commandArray[0] != "submit")
        {
            yield return "sendtochaterror @{0}, invalid command.";
            yield break;
        }
        for (int i = 0; i < commandArray[1].Length; i++)
        {
            if (!validcmds.Contains(commandArray[1][i]))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
        }
        for (int i = 0; i < commandArray[1].Length; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                if (commandArray[1][i] == validcmds[j])
                {
                    yield return null;
                    yield return new WaitForSeconds(1f);
                    buttons[j].OnInteract();
                }
                if (inputCounter == tape.Length)
                    yield return "awardpointsonsolve " + startingTime;
            }
        }
        yield break;
    }
}