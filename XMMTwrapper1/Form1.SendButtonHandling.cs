
using Microsoft.VisualStudio.OLE.Interop;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace XMMTwrapper1
{
    public partial class Form1
    {

        // Here is form code for handling the Send Buttons
        // They hold macro messages that are held in sets in the settings file
        // Each macro can be edited and stored when required

        private void MacroButton_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            MacroMessage msg = (MacroMessage)btn.Tag;

            var tags = new List<string>
            {
                "%myCall", "%DXcall", "%myName", "%myQTH",
                "%myLOC", "%RSTout", "%NumOut", "%TimOut", "%RSTin",
                "%NumIn",  "%DXloc",  "%DXname", "%DXqth", "%rn"
            };

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Available Macros Tags:");
            //sb.AppendLine("");

            for (int i = 0; i < tags.Count; i++)
            {
                sb.Append(tags[i].PadRight(14)); // PadRight keeps columns aligned

                // If we've reached 4 items, and it's not the very last item, add a newline
                if ((i + 1) % 4 == 0 && i != tags.Count - 1)
                {
                    sb.AppendLine();
                }
            }


            if (cbEditButton.Checked)
            {
                // 1. EDIT MODE
                string prompt = sb.ToString();
                string newLabel = "";
                string newTemplate = Microsoft.VisualBasic.Interaction.InputBox(
                    prompt, "Macro Editor", msg.Template);
                if (newTemplate != "")
                {
                    newLabel = Microsoft.VisualBasic.Interaction.InputBox(
                        "Edit Label:", "Macro Editor", msg.Label);
                }

                if (!string.IsNullOrEmpty(newLabel)) msg.Label = newLabel;
                
                if (!string.IsNullOrEmpty(newTemplate))  msg.Template = newTemplate;
                
                if (!string.IsNullOrEmpty(newLabel) || !string.IsNullOrEmpty(newTemplate))
                { 
                    // Save to INI using your current mode/set context
                    // Determine context based on UI selections to select correct ini data set
                    string currentContext = GetCurrentMacroContext();
                    _macroProvider.SaveMacroToIni(currentContext, msg);
                    UpdateButtonMacros();                                   //apply changes to buttons
                }
                cbEditButton.Checked = false;
            }
            else
            {
                // 2. SEND MODE
                //Special case of BARTG RTTY contest that includes HHMM after serial num
                // if no time generated yet add it to serial num. eg 001-1533
                // note serial number update resets this each time
                if (msg.Template.Contains("%TimOut") && !tbSerialNumOut.Text.Contains("-"))
                {
                    string timeStr = DateTime.Now.ToString("HHmm");
                    tbSerialNumOut.Text = $"{tbSerialNumOut.Text}-{timeStr}";
                }
                // Update variables from textboxes right before parsing
                UpdateButtonMacroInserts();
               
                if (btnCW.Checked)
                {
                    string outMsg = _macroProvider.ParseCW(msg.Template);
                    _serial.SendMessage(outMsg);
                }
                else if (btnRTTY.Checked)
                {
                    //"RTTY:{M1}{Z" + btnStr + "}{X1}"
                    string outMsg = _macroProvider.ParseRTTY(msg.Template);
                    _NamedPipeClient.SendCommand(outMsg);
                }
                else if (btnJTTY.Checked)
                {
                    string outMsg = _macroProvider.ParseJTTY(msg.Template);
                    if (outMsg == "") return;
                    _jttyLink.SendTag("TXTEXT", outMsg);   // transmit text via WS
                }
            }
        }

        //Select macro set for Send buttons that are stored in Settings file
        private void UpdateButtonMacros()
        {
            // Determine context based on UI selections to select correct ini data set
            string currentContext = GetCurrentMacroContext();

            // Get the list of messages
            var messages = _macroProvider.LoadMacroSet(currentContext, _sendButtonList.Count);

            // Loop through your button group (assuming they are in a Panel or List)
            for (int i = 0; i < messages.Count; i++)
            {
                Button btn = _sendButtonList[i]; // Or use Controls.Find()
                btn.Text = messages[i].Label;
                btn.Tag = messages[i]; // Store the whole object in the Tag
            }
        }

        // This sets up button macro substitutions dictionary with the latest values and tag names
        private void UpdateButtonMacroInserts()
        {
            // NOTE: fixed operator defaults stored in settings.ini file
            _macroProvider.AddTagSubstitutions("%myCall", _ini.GetValue("FixedSubs", "myCall", "G4AHN"));
            _macroProvider.AddTagSubstitutions("%DXcall", tbCallsign.Text);
            _macroProvider.AddTagSubstitutions("%myName", _ini.GetValue("FixedSubs", "myName", "RICH"));
            _macroProvider.AddTagSubstitutions("%myQTH", _ini.GetValue("FixedSubs", "myQTH", "FARNHAM"));
            _macroProvider.AddTagSubstitutions("%myLOC", _ini.GetValue("FixedSubs", "myLOC", "IO91OE"));

            _macroProvider.AddTagSubstitutions("%RSTout", tbReportOut.Text);
            _macroProvider.AddTagSubstitutions("%NumOut", tbSerialNumOut.Text);
            _macroProvider.AddTagSubstitutions("%TimOut", "");
            _macroProvider.AddTagSubstitutions("%RSTin", tbReportIn.Text);
            _macroProvider.AddTagSubstitutions("%NumIn", tbSerialNumIn.Text);



            _macroProvider.AddTagSubstitutions("%DXloc", tbLocator.Text);
            _macroProvider.AddTagSubstitutions("%DXname", tbOpName.Text);
            _macroProvider.AddTagSubstitutions("%DXqth", tbQTH.Text);

            _macroProvider.AddTagSubstitutions("%rn", "\r\n");
        }


        //TODO choices to be expanded
        //section name in Settings file
        public string strCurrentSendButtonMode = "default";
        private string GetCurrentMacroContext()
        {
            if (btnCW.Checked) 
            {
                return $"{"CW"}_{strCurrentSendButtonMode}";
            }
            else if (btnJTTY.Checked) 
            {
                return $"{"JTTY"}_{strCurrentSendButtonMode}";
            }
            else        // RTTY is default if neither CW or JTTY is selected
            {
                return $"{"RTTY"}_{strCurrentSendButtonMode}";
            }
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F1:
                    btnSend1.PerformClick();    // Simulates a physical mouse click
                    e.Handled = true;           // Prevents Windows from doing anything else with F1
                    break;

                case Keys.F2:
                    btnSend2.PerformClick();
                    e.Handled = true;
                    break;

                case Keys.F3:
                    btnSend3.PerformClick();
                    e.Handled = true;
                    break;

                case Keys.F4:
                    btnSend4.PerformClick();
                    e.Handled = true;
                    break;

                case Keys.F5:
                    btnSend5.PerformClick();
                    e.Handled = true;
                    break;

                case Keys.F6:
                    btnSend6.PerformClick();
                    e.Handled = true;
                    break;

                case Keys.F7:
                    btnSend7.PerformClick();
                    e.Handled = true;
                    break;
            }
        }

        private void btnStopTX_Click(object sender, EventArgs e)
        {
            if (btnCW.Checked)
            {
                
                //_serial.SendMessage("{X0}");
            }
  
            else if (btnRTTY.Checked)
            {
                //"RTTY:{M1}{Z" + btnStr + "}{X1}"
                _NamedPipeClient.SendCommand("RTTY:{X0}");
            }
            else if (btnJTTY.Checked)
            {
                _jttyLink.SendTag("ABORT");          // stop via WS
            }
        }
    }
}
