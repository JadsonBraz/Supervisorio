using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using EasyModbus;

namespace Supervisorio
{
    public partial class Form1 : Form
    {
        private const int V_MAX = 30;   // tensao maxima
        private const int A_MAX = 10;   // corrente maxima
        private const int T_MAX = 100;  // temperatura maxima

        private int cont = 0;
        
        // configs globais
        private const int AUTO = 1;
        private const int CONTINUO = 3;
        private const int PULSATIVO = 4;

        // modbus client
        ModbusClient clientModBus;

        public Form1()
        {
            InitializeComponent();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            float tensaoContinua = (float) trackBar1.Value * V_MAX / trackBar1.Maximum;
            label4.Text = String.Format("{0:N2} V", tensaoContinua);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
            }

            try
            {
                this.clientModBus = new ModbusClient(comboBox1.SelectedItem.ToString());
                this.clientModBus.Connect();
                timer1.Enabled = true;

                this.UpdateValues();    // atualiza os valores da interface grafica
            } catch
            {
                MessageBox.Show("A porta serial nao pode ser aberta", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboBox1.SelectedItem = "";
            }
        }

        private void UpdateValues()     // atualiza labels e boxes quando conectar na porta COM
        {
            int[] registradores = clientModBus.ReadHoldingRegisters(0, 11);

            // tensoes
            trackBar1.Value = registradores[3];
            numericUpDown2.Value = (decimal) registradores[3] * V_MAX / 255;
            label4.Text = String.Format("{0:N2} V", numericUpDown2.Value);

            // ton
            numericUpDown1.Value = registradores[4];
            // toff
            numericUpDown4.Value = registradores[5];

            // PID
            // Setpoint
            numericUpDown3.Value = (decimal) registradores[9] * A_MAX / 1023;
            // Kp
            numericUpDown5.Value = (decimal) registradores[6] / 100;
            // Ki
            numericUpDown6.Value = (decimal) registradores[7] / 100;
            // Kd
            numericUpDown7.Value = (decimal) registradores[8] / 100;

        }

        // atualiza lista de portas COM disponiveis
        private void button1_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            //obter as portas COM disponíveis
            String[] ports = SerialPort.GetPortNames();
            // adicionar as portas ao combobox
            foreach (string port in ports)
            {
                comboBox1.Items.Add(port);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if(clientModBus != null)
            {
                // ta travando tudo no arduino comum !
                try 
                {
                    int[] registradores = clientModBus.ReadHoldingRegisters(0, 4);

                    label14.Text = String.Format("{0:N2} V", (decimal)registradores[0] * V_MAX / 1023);
                    label15.Text = String.Format("{0:N2} V", (decimal)registradores[3] * V_MAX / 255);
                    label16.Text = String.Format("{0:N2} mA", (decimal)registradores[1] * A_MAX / 1023);
                    label17.Text = String.Format("{0:N2} °C", (decimal)registradores[2] * T_MAX / 1023);

                    label21.Text = label14.Text;
                    label22.Text = label16.Text;
                    label23.Text = label17.Text;
                } catch
                {

                }
            }

            this.cont++;
        }

        private void button2_Click(object sender, EventArgs e)      // enviar configuracao CONTINUA
        {
            if (clientModBus != null)
            {
                SetglobalConfig(CONTINUO);
                SetTensaoPWM(trackBar1.Value);
            }
        }

        private void button3_Click(object sender, EventArgs e)      // configuracao PULSATIVA
        {
            if (clientModBus != null)
            {
                SetglobalConfig(PULSATIVO);
                SetTensaoPWM(trackBar1.Value);
                SetTensaoPWM((int)numericUpDown2.Value);
                SetTempoPulso((int)numericUpDown1.Value, (int)numericUpDown4.Value);
            }
        }

        private void button4_Click(object sender, EventArgs e)      // configuracao AUTOMATICA
        {
            if (clientModBus != null)
            {
                SetglobalConfig(AUTO);
                SetPID_Vars(
                    (int)numericUpDown5.Value * 100, 
                    (int)numericUpDown6.Value * 100, 
                    (int)numericUpDown7.Value * 100, 
                    (int) (numericUpDown3.Value * 1023/A_MAX) 
                );
            }
        }

        private void SetglobalConfig(int config)
        {
            clientModBus.WriteSingleRegister(10, config);
        }


        private void SetTensaoPWM(int value)    // 8 bits
        {
            clientModBus.WriteSingleRegister(3, value);     
        }

        private void SetTempoPulso(int tOn, int tOff)
        {
            clientModBus.WriteSingleRegister(4, tOn);
            clientModBus.WriteSingleRegister(5, tOff);
        }

        private void SetPID_Vars(int Kp, int Ki, int Kd, int Setpoint)
        {
            clientModBus.WriteSingleRegister(6, Kp);
            clientModBus.WriteSingleRegister(7, Ki);
            clientModBus.WriteSingleRegister(8, Kd);
            clientModBus.WriteSingleRegister(9, Setpoint);
        }

        
    }
}
