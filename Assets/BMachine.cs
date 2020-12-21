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
    public KMBombModule module;
    public KMAudio sound;
    public KMBombInfo bomb;
    int moduleId;
    static int moduleIdCounter = 1;
    bool solved;
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
                loopstart = int.Parse(program[i].Remove(0, 1));
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
        if (!solved && command.text == "READY")
        {
            button.AddInteractionPunch();
            sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            if (tape[inputCounter] == input)
            {
                inputCounter++;
                if (inputCounter == tape.Length)
                {
                    sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
                    solved = true;
                    module.HandlePass();
                    command.text = "SOLVED";
                }
            }
            else
            {
                module.HandleStrike();
                inputCounter = 0;
            }
        }
    }
    IEnumerator ShowProgram()
    {
        int tick = 0;
        while (tick < startingTime - 1)
        {
            command.text = program[tick];
            sound.PlaySoundAtTransform("More Cowbell", transform);
            tick++;
            yield return new WaitForSeconds(30f);
        }
        command.text = "READY";
        yield break;
    }
#pragma warning disable 414
    private string TwitchHelpMessage = "use e.g. '!{0} submit 110110' to submit the tape as binary with 1 = marked and 0 = unmarked.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        string validcmds = "01";
        string[] commandArray = command.Split(' ');
        if (commandArray.Length != 2 || commandArray[0] != "submit")
        {
            yield return "sendtochaterror @{0}, invalid command.";
            yield break;
        }
        for (int i = 0; i < commandArray[2].Length; i++)
        {
            if (!validcmds.Contains(commandArray[2][i]))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
        }
        for (int i = 0; i < commandArray[2].Length; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                if (command[i] == validcmds[j])
                {
                    yield return null;
                    yield return new WaitForSeconds(1f);
                    buttons[j].OnInteract();
                }
                yield break;
            }
            
        }
    }
}