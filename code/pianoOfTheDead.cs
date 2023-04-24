using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;

using Control = GTA.Control;
using System.Media;

namespace PianoOfDeath
{
    public class PianoOfDeath : Script
    {
        Model myPedModel = PedHash.Clown01SMY; //the model of the 7 NPCs 
        List<Ped> myPeds = new List<Ped>(); //the list of NPCs
        List<Ped> myDeadPeds = new List<Ped>(); //the list to keep all the dead NPCs
        List<Prop> spotlight = new List<Prop>(); //the list of spotlight prop on top of each NPC
        Vector3 spotlightRot = new Vector3(63, 5, 0); //the rotation coordinates of each spotlight
        SoundPlayer turnOnSpot = new SoundPlayer("./scripts/tones/turnOnSpotlight.wav");
        List<Keys> toneKeys = new List<Keys>() {  Keys.F, Keys.G, Keys.H, Keys.J, Keys.K, Keys.L, Keys.Oem7 }; //mapped tone keys
        List<SoundPlayer> tones = new List<SoundPlayer>(); //the list of scream audio files
        List<Vector3> newPos = new List<Vector3>(); //the list of each NPC`s position
        bool firstTime = true; //a boolean to record the heading of each NPC at the beginning of the piano setup
        List<float> pedHeading = new List<float>(); //the list of heading for each NPC (head rotation in degrees)

        int toneCount; //how many tones
        int pianoLength = 15; //how long do the NPCs` line span

        bool pianoOn = false; //a boolean to prevent the piano being spawned when already active
       
        public PianoOfDeath()
        {
            this.Tick += onTick;
            this.KeyUp += onKeyUp;
            this.KeyDown += onKeyDown;
        }

        private void onTick(object sender, EventArgs e)
        {
    
        }

        private void onKeyUp(object sender, KeyEventArgs e)
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
            if(pianoOn)
            {
                for (int i = 0; i < toneKeys.Count; i++)
                {
                    if (myPeds != null)
                    {
                        if (e.KeyCode == toneKeys[i])
                        {
                            //play the scream sound
                            tones[i].Play();
                            //kill the ped after they sang
                            myPeds[i].Kill();
                            //create a new ped in the position of the one that just died and in the same place in the list
                            var newPed = World.CreatePed(myPedModel, newPos[i]);
                            //set the heading of the new ped
                            newPed.Heading = pedHeading[i];
                            //add the new ped to the ped list
                            myPeds[i] = newPed;
                            //add the ped to the dead ped list
                            myDeadPeds.Add(newPed);

                            //mute them
                            Function.Call(Hash.DISABLE_PED_PAIN_AUDIO, newPed, true);
                        }
                    }

                }
            }
        }

        private void onKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.I)
            {
                //set player invisible
                Function.Call(Hash.SET_ENTITY_VISIBLE, Game.Player.Character, false, 0);
                //Set time and weather
                Function.Call(Hash.SET_CLOCK_TIME, 13, 45, 00);
                Function.Call(Hash.SET_WEATHER_TYPE_NOW, "EXTRASUNNY");
            }


        }

        private void spawnPiano()
        {
            //set player invisible
            Function.Call(Hash.SET_ENTITY_VISIBLE, Game.Player.Character, false, 0);

            //switch piano ON
            pianoOn = true;
            //Set time and weather
            Function.Call(Hash._CREATE_LIGHTNING_THUNDER); // does this work?

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
                Prop newSpot = World.CreateProp("prop_spot_clamp_02", Game.Player.Character.GetOffsetInWorldCoords(new Vector3(spawnPos.X, spawnPos.Y, 5)), spotlightRot, false, false);
                //play sound of spotlight turning on
                turnOnSpot.Play();
                //add the spotlight to the list of Props spotlight
                spotlight.Add(newSpot);
                //Wait 1 sec
                Wait(1000);
                //spawn a new Ped called newPed[
                var newPed = World.CreatePed(myPedModel, Game.Player.Character.GetOffsetInWorldCoords(new Vector3(spawnPos.X, spawnPos.Y, 2.5f)));
                //wait 1 sec
                //Wait(1000);
                //turn the Ped toward the camera
                newPed.Task.TurnTo(GameplayCamera.Position);
                //store positions
                newPos.Add(newPed.Position);
                //mute pain sounds from ped
                Function.Call(Hash.DISABLE_PED_PAIN_AUDIO, newPed, true);
                //add the new Ped to my list of Peds myPeds
                myPeds.Add(newPed);
                //add the ped to the dead ped list
                myDeadPeds.Add(newPed);
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
                //UI.Notify("pedHeading = " + pedHeading[j]);
            }
        }

        private void despawnPiano()
        {
            //Set time and weather
            Function.Call(Hash.SET_CLOCK_TIME, 13, 45, 00);
            Function.Call(Hash.SET_WEATHER_TYPE_NOW, "EXTRASUNNY");

            for (int i = 0; i < toneCount; i++)
            {
                spotlight[i].Delete();
                spotlight[i] = null;
                myPeds[i].Delete();
                myPeds[i] = null;
            }
            for (int j = 0; j < myDeadPeds.Count; j++)
            {
                myDeadPeds[j].Delete();
                myDeadPeds[j] = null;
            }

            spotlight.Clear();
            myPeds.Clear();
            pedHeading.Clear();
            tones.Clear();
            Function.Call(Hash.SET_ENTITY_VISIBLE, Game.Player.Character, true, 0);
            firstTime = true;
            pianoOn = false;
        }
    }
}
