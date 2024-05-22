using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;
using System.Media;
using NAudio.Midi;

namespace PianoOfTheDead
{
    public class PianoOfTheDead : Script
    {
        
        ScriptSettings config;//ini file
        Keys keyC, keyD, keyE, keyF, keyG, keyA, keyB; //PIANO KEYS

        Model myPedModel = PedHash.Clown01SMY; //the model of the 7 NPCs 
        List<Ped> myPeds = new List<Ped>(); //the list of NPCs
        List<Ped> myDeadPeds = new List<Ped>(); //the list to keep all the dead NPCs
        int maxPeds = 200;
        List<Prop> spotlight = new List<Prop>(); //the list of spotlight prop on top of each NPC
        Vector3 spotlightRot = new Vector3(63, 5, 0); //the rotation coordinates of each spotlight
        SoundPlayer turnOnSpot = new SoundPlayer("./scripts/tones/turnOnSpotlight.wav"); //the sound effect for turning on a spotlight
        List<Keys> toneKeys = new List<Keys>();// { Keys.D, Keys.F, Keys.G, Keys.H, Keys.J, Keys.K, Keys.L }; //mapped tone keys
        List<SoundPlayer> tones = new List<SoundPlayer>(); //the list of scream audio files
        List<Vector3> newPos = new List<Vector3>(); //the list of each NPC`s position
        List<float> pedHeading = new List<float>(); //the list of heading for each NPC (head rotation in degrees)

        int toneCount; //how many tones
        int pianoLength = 15; //how long do the NPCs` line span

        bool pianoOn = false; //a boolean to prevent the piano being spawned when already active

        //MIDI stuff
        int midiOn;
        int midiDevice;
        private MidiIn midiIn;
        List<int> tonesMidi = new List<int>();
        int midiC, midiD, midiE, midiF, midiG, midiA, midiB; //PIANO MIDI NOTES
        int note = -1;

        public PianoOfTheDead()/////////////////////////////////////////////////////////////////////////////////////////
        {
            Tick += onTick;
            KeyUp += onKeyUp;

            //read from .ini
            config = ScriptSettings.Load("scripts/PianoOfTheDeadKeys.ini");

            midiOn = config.GetValue<int>("MIDI SETTINGS", "MIDI_ON", 0);
            midiDevice = config.GetValue<int>("MIDI SETTINGS", "MIDI_DEVICE", 0);

            keyC = config.GetValue<Keys>("PIANO KEYS", "C", Keys.D);
            keyD = config.GetValue<Keys>("PIANO KEYS", "D", Keys.F);
            keyE = config.GetValue<Keys>("PIANO KEYS", "E", Keys.G);
            keyF = config.GetValue<Keys>("PIANO KEYS", "F", Keys.H);
            keyG = config.GetValue<Keys>("PIANO KEYS", "G", Keys.J);
            keyA = config.GetValue<Keys>("PIANO KEYS", "A", Keys.K);
            keyB = config.GetValue<Keys>("PIANO KEYS", "B", Keys.L);

            toneKeys.Add(keyC);
            toneKeys.Add(keyD);
            toneKeys.Add(keyE);
            toneKeys.Add(keyF);
            toneKeys.Add(keyG);
            toneKeys.Add(keyA);
            toneKeys.Add(keyB);

            midiC = config.GetValue<int>("PIANO MIDI NOTES", "C", 60);
            midiD = config.GetValue<int>("PIANO MIDI NOTES", "D", 62);
            midiE = config.GetValue<int>("PIANO MIDI NOTES", "E", 64);
            midiF = config.GetValue<int>("PIANO MIDI NOTES", "F", 65);
            midiG = config.GetValue<int>("PIANO MIDI NOTES", "G", 67);
            midiA = config.GetValue<int>("PIANO MIDI NOTES", "A", 69);
            midiB = config.GetValue<int>("PIANO MIDI NOTES", "B", 71);

            tonesMidi.Add(midiC);
            tonesMidi.Add(midiD);
            tonesMidi.Add(midiE);
            tonesMidi.Add(midiF);
            tonesMidi.Add(midiG);
            tonesMidi.Add(midiA);
            tonesMidi.Add(midiB);

            if (midiOn == 1)
            {
                // Initialize MIDI input device
                midiIn = new MidiIn(midiDevice); // Change the device number if necessary
                midiIn.MessageReceived += MidiIn_MessageReceived;
                midiIn.Start();
            }
        }

        private void onTick(object sender, EventArgs e)/////////////////////////////////////////////////////////////////
        {
            if (pianoOn)
            {
                //temporarily disable player controls when the piano is on
                Game.DisableAllControlsThisFrame();
                //Game.DisableAllControlsThisFrame(2);
                //invalidate idle cam
                Function.Call(Hash.INVALIDATE_IDLE_CAM);
                Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);

                //midi note out
                if (note >= 0)
                {
                    //GTA.UI.Screen.ShowHelpText("note = " + note, -1, false, false);
                    playPiano(0, note);
                }
            }
        }

        private void onKeyUp(object sender, KeyEventArgs e)/////////////////////////////////////////////////////////////
        {
            //SHIFT + Y spawn the piano:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
            if (e.KeyCode == Keys.Y && e.Modifiers == Keys.Shift && !pianoOn)
            {
                spawnPiano();
            }

            //SHIFT + Z despawn piano and show player again:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
            if (e.KeyCode == Keys.Z && e.Modifiers == Keys.Shift && pianoOn)
            {
                despawnPiano();
            }

            //if piano keys are played:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
            if (pianoOn)
            {
                playPiano(e.KeyCode, 0);
            }
        }

        private void playPiano(Keys myKey, int myMidi)//////////////////////////////////////////////////////////////////////////////////////////
        {
            for (int i = 0; i < toneKeys.Count; i++)
            {
                if (myPeds != null)
                {
                    if (myKey == toneKeys[i] || myMidi == tonesMidi[i])
                    {
                        //play the scream sound
                        tones[i].Play();
                        //kill the ped after it sang
                        myPeds[i].Kill();
                        //add the dead ped to the list of dead peds
                        myDeadPeds.Insert(0, myPeds[i]);
                        //create a new ped in the position of the one that just died and in the same place in the list
                        var newPed = World.CreatePed(myPedModel, newPos[i]);
                        //set the heading of the new ped
                        newPed.Heading = pedHeading[i];


                        if (myDeadPeds.Count > maxPeds)
                        {
                            myDeadPeds[maxPeds].Delete();
                            myDeadPeds.RemoveAt(maxPeds);
                        }

                        //add the new ped to the ped list
                        myPeds[i] = newPed;

                        //mute them
                        Function.Call(Hash.DISABLE_PED_PAIN_AUDIO, newPed, true);
                        //reset midi note
                        note = -1;

                        //GTA.UI.Screen.ShowHelpText("my peds = " + myPeds.Count + " my dead peds = " + myDeadPeds.Count);
                    }
                }
            }
        }
        private void MidiIn_MessageReceived(object sender, MidiInMessageEventArgs e)///////////////////////////////////
        {
            // Extract MIDI message data
            var message = e.MidiEvent as NoteEvent;
            if (message != null && message.CommandCode == MidiCommandCode.NoteOn)
            {
                // Handle NoteOn MIDI message
                int noteNumber = message.NoteNumber;
                int velocity = message.Velocity;

                note = noteNumber;
            }
        }
        private void spawnPiano()///////////////////////////////////////////////////////////////////////////////////////
        {
            //set player invisible
            Function.Call(Hash.SET_ENTITY_VISIBLE, Game.Player.Character, false, 0);

            //switch piano ON
            pianoOn = true;

            //Set time and weather
            Function.Call(Hash.SET_WEATHER_TYPE_NOW, "THUNDER");
            Function.Call(Hash.SET_CLOCK_TIME, 23, 45, 00);
            Function.Call(Hash.PAUSE_CLOCK, true);

            Wait(2000);

            toneCount = toneKeys.Count;


            for (int i = 0; i < toneCount; i++)
            {
                //add a placeholder value to the heading list
                pedHeading.Add(0);
                //total piano length / number of keys = distance among keys
                float pianoSpacing = pianoLength / toneCount;
                //spawn a new Spotlight called newSpot
                Vector2 spawnPos = new Vector2(-(pianoLength / 2) + (i * pianoSpacing) + 1, 6);
                Prop newSpot = World.CreateProp("prop_spot_clamp_02", Game.Player.Character.GetOffsetPosition(new Vector3(spawnPos.X, spawnPos.Y, 5)), spotlightRot, false, false);
                //play sound of spotlight turning on
                turnOnSpot.Play();
                //add the spotlight to the list of Props spotlight
                spotlight.Add(newSpot);
                //Wait 1 sec
                Wait(1000);
                //spawn a new Ped called newPed[
                var newPed = World.CreatePed(myPedModel, Game.Player.Character.GetOffsetPosition(new Vector3(spawnPos.X, spawnPos.Y, 2.5f)));
                //turn the Ped toward the camera
                newPed.Task.TurnTo(GameplayCamera.Position);
                //store positions
                newPos.Add(newPed.Position);
                //mute pain sounds from ped
                Function.Call(Hash.DISABLE_PED_PAIN_AUDIO, newPed, true);
                //add the new Ped to my list of Peds myPeds
                myPeds.Add(newPed);
                    //add the ped to the dead ped list
                    //myDeadPeds.Add(newPed);
                //create a new tone
                var newTone = new SoundPlayer("./scripts/tones/" + i + ".wav");
                //add it to the tone list
                tones.Add(newTone);
            }
            //wait untill all NPCs have turned to the camera to record the right heading value
            Wait(3000);
            for (int j = 0; j < toneCount; j++)
            {
                pedHeading[j] = myPeds[j].Heading;
            }
        }

        private void despawnPiano()/////////////////////////////////////////////////////////////////////////////////////
        {
            //Set time and weather
            Function.Call(Hash.SET_CLOCK_TIME, 13, 45, 00);
            Function.Call(Hash.SET_WEATHER_TYPE_NOW, "EXTRASUNNY");

            //delete and clear the 7 NPCs alive amd the spotlights
            for (int i = 0; i < toneCount; i++)
            {
                spotlight[i].Delete();
                spotlight[i] = null;
                myPeds[i].Delete();
                myPeds[i] = null;
            }
            //delete all the dead NPCs
            for (int j = 0; j < myDeadPeds.Count; j++)
            {
                myDeadPeds[j].Delete();
                myDeadPeds[j] = null;
            }


            //clear all the lists
            spotlight.Clear();
            myPeds.Clear();
            myDeadPeds.Clear();
            pedHeading.Clear();
            tones.Clear();
            newPos.Clear();
            //set the character visible again
            Function.Call(Hash.SET_ENTITY_VISIBLE, Game.Player.Character, true, 0);
            //reset initial booleans
            pianoOn = false;
        }
    }
}
