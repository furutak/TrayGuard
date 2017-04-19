using System;
using System.Data;
using System.Windows.Forms;
using System.Security.Permissions;
using System.Drawing;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace TrayGuard
{
    public partial class frmCartonAdjust : Form
    {
        //�e�t�H�[��frmTray�փC�x���g������A���i�f���Q�[�g�j
        public delegate void RefreshEventHandler(object sender, EventArgs e);
        public event RefreshEventHandler RefreshEvent;

        //���̑��񃍁[�J���ϐ�
        DataTable dtCarton;
        string palletId;
        string user;
        string mode;
        bool cartonNg;
        bool sound;
        
        
        // �R���X�g���N�^
        public frmCartonAdjust()
        {
            InitializeComponent();
        }

        // ���[�h���̏���
        private void frmCartonAdjust_Load(object sender, EventArgs e)
        {
            //�t�H�[���̏ꏊ���w��
            this.Left = 450;
            this.Top = 100;

            changeFormatByMode();
            dtCarton = new DataTable();
            defineDatatable(ref dtCarton);
            updateDataGridViews(dtCarton, ref dgvCarton);
        }

        // �T�u�v���V�[�W��: �c�s�̒�`
        private void defineDatatable(ref DataTable dt)
        {
            dt.Columns.Add("carton_id", typeof(string));
            dt.Columns.Add("lot", typeof(string));
            dt.Columns.Add("m_qty", typeof(int));
            dt.Columns.Add("batch", typeof(string));
            dt.Columns.Add("register_date", typeof(DateTime));
            dt.Columns.Add("rg_user", typeof(string));
            dt.Columns.Add("multi_lot", typeof(string));
            dt.Columns.Add("check", typeof(string));
        }

        // �T�u�v���V�[�W���F�e�t�H�[���ŌĂяo���A�e�t�H�[���̏����A�e�L�X�g�{�b�N�X�֊i�[���Ĉ����p��
        public void updateControls(string pallet, string carton, int row, string userInfo, string modeSelected)
        {
            palletId = pallet;
            txtBefore.Text = carton;
            txtRow.Text = row.ToString();
            user = userInfo;
            mode = modeSelected;
        }

        // �T�u�v���V�[�W���F �u�����[�h�A�폜���[�h�A�ǉ����[�h�̐؂�ւ�
        private void changeFormatByMode()
        {
            // �u�����[�h�̏ꍇ
            if (mode == "replace")
            {
                this.Text = "Replace Carton";
                btnReplaceCarton.Visible = true;
                btnDeleteCarton.Visible = false;
                btnAddCarton.Visible = false;
                txtAfter.Enabled = true;
            }
            // �폜���[�h�̏ꍇ
            else if (mode == "delete")
            {
                this.Text = "Delete Carton";
                btnReplaceCarton.Visible = false;
                btnDeleteCarton.Visible = true;
                btnAddCarton.Visible = false;
                txtAfter.Enabled = false;
            }
            // �ǉ����[�h�̏ꍇ
            else if (mode == "add")
            {
                this.Text = "Delete Carton";
                btnReplaceCarton.Visible = false;
                btnDeleteCarton.Visible = false;
                btnAddCarton.Visible = true;
                txtAfter.Enabled = true;
            }
        }

        // �T�u�v���V�[�W���F�f�[�^�O���b�g�r���[�̍X�V
        private void updateDataGridViews(DataTable dt, ref DataGridView dgv)
        {
            // �f�[�^�O���b�g�r���[�ւc�s�`�`�s�`�a�k�d���i�[
            dgv.DataSource = dt;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            // �e�X�g���ʂ��e�`�h�k�܂��̓��R�[�h�Ȃ��̃V���A�����}�[�L���O����
            if (dt.Rows.Count <= 0) return;
            if (dgv["check", 0].Value.ToString() != "OK" || dgv["lot", 0].Value.ToString() == string.Empty)
            {
                for (int i = 0; i < dgv.ColumnCount; i++)
                {
                    dgv[i, 0].Style.BackColor = Color.Red;
                }
                soundAlarm();
                cartonNg = true;
            }
            else
            {
                for (int i = 0; i < dgv.ColumnCount; i++)
                {
                    dgv[i, 0].Style.BackColor = Color.FromKnownColor(KnownColor.Window);
                }
                cartonNg = false;
            }
        }

        // �ύX�ヂ�W���[�����X�L�������ꂽ�Ƃ��̏���
        private void txtAfter_KeyDown(object sender, KeyEventArgs e)
        {
            // �G���^�[�L�[�̏ꍇ�A�e�L�X�g�{�b�N�X�̌������P�T���̏ꍇ�̂݁A�������s��
            if (e.KeyCode != Keys.Enter || txtAfter.Text.Length != 15) return;

            // �u�����[�h�A�܂��́A�ǉ����[�h�̏ꍇ�̂݁A�������s��
            if (mode == "replace" || mode == "add")
            {
                string carton = txtAfter.Text;
                string sql = "select carton_id, lot, m_qty, batch, register_date, rg_user, " +
                    "case when l_cnt >= 2 then 'T' else 'F' end as multi_lot, " +
                    "case when cancel_date is not null then to_char(cancel_date,'YYYY/MM/DD') " +
                         "when pallet_id is not null then pallet_id else 'OK' end as check " +
                    "from t_carton where carton_id='" + carton + "'";
                System.Diagnostics.Debug.Print(sql);
                DataTable dt = new DataTable();
                TfSQL tf = new TfSQL();
                tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql, ref dt);

                // �e�X�^�[�f�[�^�ɊY�����Ȃ��ꍇ�ł��A���[�U�[�F���p�ɕ\�����邽�߂̏���
                dtCarton.Rows.Clear();
                DataRow dr = dtCarton.NewRow();
                dr["carton_id"] = carton;
                // �e�X�^�[�f�[�^�ɊY��������ꍇ�̏���
                if (dt.Rows.Count != 0)
                {
                    dr["lot"] = (string)dt.Rows[0]["lot"];
                    dr["m_qty"] = (int)dt.Rows[0]["m_qty"];
                    dr["batch"] = (string)dt.Rows[0]["batch"];
                    dr["register_date"] = (DateTime)dt.Rows[0]["register_date"];
                    dr["rg_user"] = (string)dt.Rows[0]["rg_user"];
                    dr["multi_lot"] = (string)dt.Rows[0]["multi_lot"];
                    dr["check"] = (string)dt.Rows[0]["check"];
                }

                // ��������̃e�[�u���Ƀ��R�[�h��ǉ�
                dtCarton.Rows.Add(dr);

                // �f�[�^�O���b�g�r���[�̍X�V
                updateDataGridViews(dtCarton, ref dgvCarton);
            }
        }

        // �o�^�ς݂̃J�[�g������т��̕t�я����A�t�o�c�`�s�d���Œu��������
        private void btnReplace_Click(object sender, EventArgs e)
        {
            if (cartonNg || dtCarton.Rows.Count <= 0) return;  

            string mdlBefore = txtBefore.Text;
            string mdlAfter = dtCarton.Rows[0]["carton_id"].ToString();

            // �X�V����
            TfSQL tf = new TfSQL();
            bool res = tf.sqlReplaceCartonOnPallet(palletId, txtBefore.Text, txtAfter.Text, user);
        
            if (res)
            {
                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                btnReplaceCarton.Enabled = false;
                txtAfter.Enabled = false;
                // txtRow.Text = string.Empty;
                this.Focus();
                MessageBox.Show("The replacement was successful." + Environment.NewLine + 
                    "Please re-print the pallet label.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
            }
        }

        // �o�^�ς݂̃p���b�g�ɑ΂��A�J�[�g����ǉ�����
        private void btnAddCarton_Click(object sender, EventArgs e)
        {
            if (cartonNg || dtCarton.Rows.Count <= 0) return;

            string mdlAfter = dtCarton.Rows[0]["carton_id"].ToString();
            string sql = "update t_carton set pallet_id = '" + palletId + "' where carton_id = '" + mdlAfter + "'";
            System.Diagnostics.Debug.Print(sql);
            TfSQL tf = new TfSQL();
            bool res = tf.sqlExecuteNonQuery(sql, false);

            if (res)
            {
                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                btnReplaceCarton.Enabled = false;
                txtAfter.Enabled = false;
                // txtRow.Text = string.Empty;
                this.Focus();
                MessageBox.Show("The addition was successful." + Environment.NewLine +
                    "Please re-print the pallet label.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
            }
        }

        // �o�^�ς݂̃p���b�g�ɑ΂��A�I�����ꂽ�J�[�g�����폜����
        private void btnDeleteCarton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtBefore.Text)) return;

            string mdlBefore = txtBefore.Text;
            string sql = "update t_carton set pallet_id = null where carton_id = '" + mdlBefore + "'";
            System.Diagnostics.Debug.Print(sql);
            TfSQL tf = new TfSQL();
            bool res = tf.sqlExecuteNonQuery(sql, false);

            if (res)
            {
                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                btnReplaceCarton.Enabled = false;
                btnDeleteCarton.Enabled = false;
                btnAddCarton.Enabled = false;
                txtAfter.Enabled = false;
                // txtRow.Text = string.Empty;
                this.Focus();
                MessageBox.Show("The deletion was successful." + Environment.NewLine +
                    "Please re-print the pallet label.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
            }
        }

        //����{�^����V���[�g�J�b�g�ł̏I���������Ȃ�
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x112;
            const long SC_CLOSE = 0xF060L;
            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt64() & 0xFFF0L) == SC_CLOSE) { return; }
            base.WndProc(ref m);
        }

        // ���̃t�H�[���Ƃ̐���������邽�߁A�L�����Z���{�^����݂���
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //MP3�t�@�C���i����͌x�����j���Đ�����
        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        private static extern int mciSendString(String command,
           StringBuilder buffer, int bufferSize, IntPtr hwndCallback);

        private string aliasName = "MediaFile";

        private void soundAlarm()
        {
            string currentDir = System.Environment.CurrentDirectory;
            string fileName = currentDir + @"\warning.mp3";
            string cmd;

            if (sound)
            {
                cmd = "stop " + aliasName;
                mciSendString(cmd, null, 0, IntPtr.Zero);
                cmd = "close " + aliasName;
                mciSendString(cmd, null, 0, IntPtr.Zero);
                sound = false;
            }

            cmd = "open \"" + fileName + "\" type mpegvideo alias " + aliasName;
            if (mciSendString(cmd, null, 0, IntPtr.Zero) != 0) return;
            cmd = "play " + aliasName;
            mciSendString(cmd, null, 0, IntPtr.Zero);
            sound = true;
        }
    }
}