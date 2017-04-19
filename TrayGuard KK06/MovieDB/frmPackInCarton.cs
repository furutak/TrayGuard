using System;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TrayGuard
{
    public partial class frmPackInCarton : Form
    {
        //�e�t�H�[��frmTray�փC�x���g������A���i�f���Q�[�g�j
        public delegate void RefreshEventHandler(object sender, EventArgs e);
        public event RefreshEventHandler RefreshEvent;

        //�f�[�^�O���b�h�r���[�p�{�^��
        DataGridViewButtonColumn openPack;

        // �v�����g�p�e�L�X�g�t�@�C���̕ۑ��p�t�H���_���A��{�ݒ�t�@�C���Őݒ肷��
        string appconfig = System.Environment.CurrentDirectory + "\\info.ini";
        

        //���̑��A�񃍁[�J���ϐ�
        DataTable dtPack;
        DataTable dtLot;
        bool formAddMode;
        bool formReprintMode;
        bool cartonIdCanceled;
        bool cartonIdPalleted;
        string userRole;
        int okCount;
        int capacity = 2;
        string maxLot;
        DateTime registerDate;
        bool sound;
        int position;

        // �R���X�g���N�^
        public frmPackInCarton()
        {
            InitializeComponent();
        }

        // ���[�h���̏���
        private void frmPackInCarton_Load(object sender, EventArgs e)
        {
            this.Text = this.Text + " " + Assembly.GetExecutingAssembly().GetName().Version;
            // ���t�H�[���̕\���ꏊ���w��
            this.Left = 250;
            this.Top = 20;
            if (position == 2) { this.Left = 400; this.Top = 30; }
            else if (position == 3) { this.Left = 450; this.Top = 40; }
            else if (position == 4) { this.Left = 500; this.Top = 50; }

            // �e�폈���p�̃e�[�u���𐶐����A�f�[�^��ǂݍ���
            dtPack = new DataTable();
            definePackTable(ref dtPack);
            if (!formAddMode) readPackInfo(ref dtPack);

            // �O���b�g�r���[�̍X�V
            updateDataGridViews(dtPack, ref dgvPack, true);

            // �ǉ����[�h�A�{�����[�h�̐؂�ւ�
            changeMode();
        }

        // �T�u�v���V�[�W���F �ǉ����[�h�A�{�����[�h�̐؂�ւ�
        private void changeMode()
        {
            // �ǉ����[�h�̏ꍇ
            if (formAddMode)
            {
                txtPack.Enabled = true;
                btnRegisterCarton.Text = "Register Carton";
                btnRegisterCarton.Enabled = false;
                btnCancelCarton.Enabled = false;
                btnClose.Enabled = true;
                btnDeleteSelection.Enabled = true;
            }
            // �{�����[�h�̏ꍇ
            else
            {
                txtPack.Enabled = false;
                btnRegisterCarton.Text = "Re-Print Label";
                btnRegisterCarton.Enabled = cartonIdCanceled ? false : true;
                // 2016.09.24 FUJII  �ăv�����g���[�h��ǉ��ifrmPack�ɂāA�g���[�h�c���L�[�Ƃ��āA�p�b�N�h�c����肵���ꍇ�̂݁A�Ĉ���\�j
                //btnRegisterCarton.Enabled = cartonIdCanceled ? false : ((formReprintMode && userRole == "super") ? true : false);
                // 2017.01.18 FUJII  �ăv�����g���[�h��ύX�i���[�U�[�����A�Ĉ�����[�h������p�~�j
                btnRegisterCarton.Enabled = cartonIdCanceled ? false : true;

                btnCancelCarton.Enabled = false;
                btnClose.Enabled = true;
                btnDeleteSelection.Enabled = false;
                if (userRole == "super") btnCancelCarton.Enabled = (cartonIdCanceled || cartonIdPalleted) ? false : true;
            }
        }

        // �ݒ�e�L�X�g�t�@�C���̓ǂݍ���
        private string readIni(string s, string k, string cfs)
        {
            StringBuilder retVal = new StringBuilder(255);
            string section = s;
            string key = k;
            string def = String.Empty;
            int size = 255;
            int strref = GetPrivateProfileString(section, key, def, retVal, size, cfs);
            return retVal.ToString();
        }
        // Windows API ���C���|�[�g
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filepath);

        // �T�u�v���V�[�W���F�e�t�H�[���ŌĂяo���A�e�t�H�[���̏����A�e�L�X�g�{�b�N�X�֊i�[���Ĉ����p��
        public void updateControls(string pid, DateTime cdate, string uid, string uname, string udept, string urole, string shift, 
            bool addMode, bool returnMode, bool canceled, bool palleted, int pos, bool reprintMode)
        {
            txtCarton.Text = pid;
            dtpRegisterDate.Value = cdate;
            txtLoginName.Text = uname;
            txtLoginDept.Text = udept;
            userRole = urole;
            formAddMode = addMode;
            cartonIdCanceled = canceled;
            cartonIdPalleted = palleted;
            position = pos;
            formReprintMode = reprintMode;
        }

        // �T�u�v���V�[�W���F�f�[�^�e�[�u���̒�`
        private void definePackTable(ref DataTable dt)
        {
            dt.Columns.Add("pack_id", typeof(string));
            dt.Columns.Add("lot", typeof(string));
            dt.Columns.Add("m_qty", typeof(int));
            dt.Columns.Add("batch", typeof(string));
            dt.Columns.Add("register_date", typeof(DateTime));
            dt.Columns.Add("rg_user", typeof(string));
            dt.Columns.Add("multi_lot", typeof(string));
            dt.Columns.Add("check", typeof(string));
        }

        // �T�u�v���V�[�W���F�c�a����f�[�^�e�[�u���ւ̓ǂݍ���
        private void readPackInfo(ref DataTable dt)
        {
            dt.Rows.Clear();
            string sql = "select pack_id, lot, m_qty, batch, register_date, rg_user, " +
                "case when l_cnt >= 2 then 'T' else 'F' end as multi_lot, " +
                "'OK' as check from t_pack where carton_id='" + txtCarton.Text + "'";
            TfSQL tf = new TfSQL();
            System.Diagnostics.Debug.Print(sql);
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql, ref dt);
        }


        // �T�u�v���V�[�W���F�f�[�^�O���b�g�r���[�̍X�V
        private void updateDataGridViews(DataTable dt1, ref DataGridView dgv1, bool load)
        {
            // �f�[�^�O���b�g�r���[�ւc�s�`�`�s�`�a�k�d���i�[
            dgv1.DataSource = dt1;
            dgv1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            // �e�X�g���ʂ��e�`�h�k�܂��̓��R�[�h�Ȃ��̃V���A�����}�[�L���O���� 
            if(formAddMode) colorViewForCanceledAndCartoned(ref dgv1);

            // �d�����R�[�h�A����тP�Z���Q�d���͂��}�[�L���O����
            if (formAddMode) colorViewForDuplicatePack(ref dgv1);

            // �O���b�g�r���[�E�[�Ƀ{�^����ǉ��i����̂݁j
            if (load && !formAddMode) addButtonsToDataGridView(dgv1);

            //�s�w�b�_�[�ɍs�ԍ���\������
            for (int i = 0; i < dgv1.Rows.Count; i++) dgv1.Rows[i].HeaderCell.Value = (i + 1).ToString();

            //�s�w�b�_�[�̕����������߂���
            dgv1.AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders);

            // ���݂̈ꎞ�o�^������ϐ��֕ێ�����
            okCount = getOkCount(dt1);
            txtOkCount.Text = okCount.ToString() + "/" + capacity.ToString();

            // ���b�g�W�v�O���b�h�r���[���X�V���A���ʂ̍ł��������b�g��ێ�����
            maxLot = updateLotSummary(dt1);

            // ���R�[�h���ƃg���C�e�ʂɂ��A�{�^���ƃR���g���[���̐���A�i�ǉ����[�h�̏ꍇ�j
            if (formAddMode)
            {
                if (okCount == capacity)
                {
                    txtPack.Enabled = false;
                    btnRegisterCarton.Enabled = true;
                    btnDeleteSelection.Enabled = true;
                }
                else
                {
                    txtPack.Enabled = true;
                    btnRegisterCarton.Enabled = false;
                    btnDeleteSelection.Enabled = true;
                    txtPack.SelectAll(); // �A���X�L�����p
                }
            }

            // �o�b�`�e�L�X�g�{�b�N�X�ցA�O���b�h�r���[�̃o�b�`��\������i�����o�b�`�����̏ꍇ�́A�x������j
            if(dt1.Rows.Count >= 1) txtBatch.Text = dt1.Rows[0]["batch"].ToString();
            if (dt1.Rows.Count == 2 && dt1.Rows[0]["batch"].ToString() != dt1.Rows[dt1.Rows.Count-1]["batch"].ToString())
                { txtBatch.Text = "Error"; soundAlarm();} 
        }

        // �T�u�T�u�v���V�[�W���F�O���b�g�r���[�E�[�Ƀ{�^����ǉ�
        private void addButtonsToDataGridView(DataGridView dgv)
        {
            openPack = new DataGridViewButtonColumn();
            openPack.HeaderText = string.Empty;
            openPack.Text = "Open";
            openPack.UseColumnTextForButtonValue = true;
            openPack.Width = 80;
            dgv.Columns.Add(openPack);
        }

        // �T�u�v���V�[�W���F�V���A���ԍ��d���Ȃ��̂o�`�r�r�����擾����
        private int getOkCount(DataTable dt)
        {
            DataTable distinct = dt.DefaultView.ToTable(true, new string[] { "pack_id", "check" });
            DataRow[] dr = distinct.Select("check = 'OK'");
            return dr.Length;
        }

        // �T�u�v���V�[�W���F���b�g�W�v�O���b�h�r���[���X�V���A���ʂ̍ł��������b�g�ԍ���Ԃ�
        public string updateLotSummary(DataTable dt)
        {
            if (dt.Rows.Count <= 0)
            {
                dtLot = new DataTable();
                dgvLotSummary.DataSource = dtLot;
                return string.Empty;
            }

            // �e�p�b�N�Ɋ܂܂��A�g���[�h�c���X�g���擾����
            string sql1 = "select tray_id from t_tray where pack_id in (";
            string sql2 = string.Empty;
            var query1 = dt.AsEnumerable().Select(row => new { pack_id = row.Field<string>("pack_id") });
            foreach (var q in query1) sql2 += "'" + q.pack_id + "', ";
            string sql3 = sql1 + VBS.Left(sql2, sql2.Length - 2) + ")";
            System.Diagnostics.Debug.Print(sql3);
            DataTable dtTemp1 = new DataTable();
            TfSQL tf = new TfSQL();
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql3, ref dtTemp1);

            // ��L�Ŏ擾�����g���[�h�c�������W���[������A���b�g�W�v���쐬����
            string sql4 = "select lot, count(lot) as qty from t_module where tray_id in (";
            string sql5 = string.Empty;
            var query2 = dtTemp1.AsEnumerable().Select(row => new { tray_id = row.Field<string>("tray_id") });
            foreach (var q in query2) sql5 += "'" + q.tray_id + "', ";
            string sql6 = sql4 + VBS.Left(sql5, sql5.Length - 2) + ") group by lot order by qty desc, lot ";
            System.Diagnostics.Debug.Print(sql6);
            DataTable dtTemp2 = new DataTable();
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql6, ref dtTemp2);

            // ���b�g�W�v�\���e�[�u���ɁA�ꎞ�e�[�u���̏����ڂ�
            dgvLotSummary.DataSource = null;
            dgvLotSummary.Refresh();
            dtLot = new DataTable();
            var query3 = dtTemp2.AsEnumerable().Select(r => new {lot = r.Field<string>("lot"), qty = r.Field<Int64>("qty") });
            // ��̒ǉ�
            foreach (var q in query3) dtLot.Columns.Add(q.lot, typeof(int));
            dtLot.Columns.Add("total", typeof(int));
            // �s�̒ǉ�
            dtLot.Rows.Add(); 
            foreach (var q in query3) dtLot.Rows[0][q.lot] = q.qty;
            dtLot.Rows[0]["total"] = query3.Sum(a => a.qty);
 
            dgvLotSummary.DataSource = dtLot;
            return query3.First().lot;
        }

        // �V���A�����X�L�������ꂽ���̏���            
        private void txtModuleId_KeyDown(object sender, KeyEventArgs e)
        {
            // �G���^�[�L�[�̏ꍇ�A�e�L�X�g�{�b�N�X�̌������P�T���̏ꍇ�̂݁A�������s��
            if (e.KeyCode != Keys.Enter || txtPack.Text.Length != 15) return;

            string pack = txtPack.Text;
            string sql = "select pack_id, lot, m_qty, batch, register_date, rg_user, " +
                "case when l_cnt >= 2 then 'T' else 'F' end as multi_lot, " +
                "case when cancel_date is not null then to_char(cancel_date,'YYYY/MM/DD') " +
                     "when carton_id is not null then carton_id else 'OK' end as check " +
                "from t_pack where pack_id='" + pack + "'";
            System.Diagnostics.Debug.Print(sql);
            DataTable dt = new DataTable();
            TfSQL tf = new TfSQL();
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql, ref dt);

            // �e�X�^�[�f�[�^�ɊY�����Ȃ��ꍇ�ł��A���[�U�[�F���p�ɕ\�����邽�߂̏���
            DataRow dr = dtPack.NewRow();
            dr["pack_id"] = pack;
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
            dtPack.Rows.Add(dr);

            // �f�[�^�O���b�g�r���[�̍X�V
            updateDataGridViews(dtPack, ref dgvPack, false);
        }

        // �o�^�{�^���������A�e��m�F�A�{�b�N�X�h�c�̔��s�A�V���A���̓o�^�A�o�[�R�[�h���x���̃v�����g���s��
        private void btnRegisterTray_Click(object sender, EventArgs e)
        {
            //�{�����[�h�̏ꍇ�́A�v�����g�A�E�g
            if (!formAddMode)
            {
                string carton = txtCarton.Text;
                TfSato tfs = new TfSato();

                // �r���`���A�r���a���A���[�U�[�ɑI��������
                //DialogResult binResult = MessageBox.Show("Please click YES for Bin A, NO for Bin B.", "Print Option",
                //    MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                //string userBin = (binResult == DialogResult.Yes) ? "A" : "B";
                string userBin = txtBatch.Text;

                // �y�K�g�����p���ۂ��A���[�U�[�ɑI��������
                //DialogResult result = MessageBox.Show("Do you print Pegatoron label also?", "Print Option", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                //if (result == DialogResult.Yes)
                //{
                tfs.printStart("packCartonInternal", carton, dtLot, txtBatch.Text, dtpRegisterDate.Value, "Carton", "Pega", 2, userBin);
                tfs.printStart("packCartonPega", carton, dtLot, txtBatch.Text, dtpRegisterDate.Value, "Carton", "Pega", 1, userBin);
                //}
                //else
                //{
                //    tfs.printStart("packCartonInternal", carton, dtLot, txtBatch.Text, dtpRegisterDate.Value, "Carton", "Fox", 2, userBin);
                //}
                return;
            }

            //�����̃o�b�`���������Ă���ꍇ�́A�x������
            if (txtBatch.Text == "Error")
            {
                MessageBox.Show("You can not register 2 batchs in a carton.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // �o�^�������́A����{�^���������A���ׂẴR���g���[���𖳌��ɂ���
            txtPack.Enabled = false;
            btnRegisterCarton.Enabled = false;
            btnDeleteSelection.Enabled = false;

            //�J�[�g���h�c�̐V�K�̔�
            // 2016.08.22 FUJII  �J�[�g���h�c�̐V�K�̔ԃv���V�[�W���ugetNewCartonId�v���A�g�����U�N�V���������o�[�W�����֕ύX
            //string cartonNew = getNewCartonId(txtBatch.Text, txtLoginName.Text);
            TfSQL tf = new TfSQL();
            string cartonNew = tf.sqlGetNewCartonId(maxLot, txtBatch.Text, txtLoginName.Text, dtLot, ref registerDate);
            if (cartonNew == string.Empty)
            {
                MessageBox.Show("An error happened in the carton id issuing process.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            //�p�b�N�e�[�u���̃t�B�[���h�b�`�q�s�n�m�h�c�A���̑����X�V����
            bool res = tf.sqlMultipleUpdatePackInCarton(dtPack, cartonNew);

            if (res)
            {
                //�o�^�ς݂̏�Ԃ�\��
                txtCarton.Text = cartonNew;
                dtpRegisterDate.Value = registerDate;

                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                this.Focus();
                MessageBox.Show("Carton ID: " + cartonNew + Environment.NewLine +
                    "and its packs were registered.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //���[�U�[�ɂ�郁�b�Z�[�W�{�b�N�X�m�F��̏���
                txtCarton.Text = String.Empty;
                txtPack.Text = String.Empty;
                dtPack.Clear();
                updateDataGridViews(dtPack, ref dgvPack, false);
            }
        }

        // �X�[�p�[���[�U�[�Ɍ���A�o�^�σJ�[�g�����L�����Z���ł���i�C���{�C�X������͕s�j
        private void btnCancelCarton_Click(object sender, EventArgs e)
        {
            // �{���ɍ폜���Ă悢���A�Q�d�Ŋm�F����B
            DialogResult result1 = MessageBox.Show("Do you really want to cancel this tray?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result1 == DialogResult.No) return;

            DialogResult result2 = MessageBox.Show("Are you really sure? Please select NO if you are not sure.", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result2 == DialogResult.No) return;

            // �L�����Z���̎��s
            string cartonId = txtCarton.Text;
            TfSQL tf = new TfSQL();
            bool res = tf.sqlCancelPackInCarton(dtPack, cartonId, txtLoginName.Text);
            if (res)
            {
                //�{�t�H�[���̃f�[�^�O���b�g�r���[�X�V
                dtPack.Clear();
                updateDataGridViews(dtPack, ref dgvPack, false);

                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                this.Focus();
                MessageBox.Show("Carton ID " + cartonId + " and its packs were canceled.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // ���b�Z�[�W�{�b�N�X�̊m�F��A����
                Close();
            }
            else
            {
                MessageBox.Show("Cancel process was not successful.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ���t�H�[���̃N���[�Y�i���̃t�H�[���ƌ`�������킹�Ă��邾���ŁA�Ӗ��͂Ȃ��j
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        // �T�u�v���V�[�W���F�L�����Z���ς݂܂��̓J�[�g���ς݂̃p�b�N���}�[�L���O����
        private void colorViewForCanceledAndCartoned(ref DataGridView dgv)
        {
            for (int i = 0; i < dgv.Rows.Count; ++i)
            {
                if (dgv["check", i].Value.ToString() != "OK")
                {
                    dgv["check", i].Style.BackColor = Color.Red;
                    soundAlarm();
                }
                else
                {
                    dgv["check", i].Style.BackColor = Color.FromKnownColor(KnownColor.Window);
                }
            }
        }

        // �T�u�v���V�[�W���F�d�����R�[�h�A�܂��͂P�Z���Q�d���͂��}�[�L���O����
        private void colorViewForDuplicatePack(ref DataGridView dgv)
        {
            if (dgv.Rows.Count <= 0) return;

            DataTable dt = ((DataTable)dgv.DataSource).Copy();
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                string pack = dgv["pack_id", i].Value.ToString();
                DataRow[] dr = dt.Select("pack_id = '" + pack + "'");
                if (dr.Length >= 2)
                {
                    dgv["pack_id", i].Style.BackColor = Color.Red;
                    soundAlarm();
                }
                else
                {
                    dgv["pack_id", i].Style.BackColor = Color.FromKnownColor(KnownColor.Window);
                }
            }
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

        // �f�[�^�O���b�h�r���[�̃_�u���N���b�N���A�f�[�^���G�N�Z���փG�N�X�|�[�g
        private void dgvPack_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            DataTable dt = new DataTable();
            dt = (DataTable)dgvPack.DataSource;
            ExcelClass xl = new ExcelClass();
            xl.ExportToExcel(dt);
        }

        // �k�n�s�W�v�O���b�h�r���[���̃{�^�����������A�Ώۂ̃��W���[�����G�N�Z���t�@�C���֏o�͂���
        private void btnExportModule_Click(object sender, EventArgs e)
        {
            // 2016.08.29 FUJII �ʃX���b�h�ŏ����i�����X�s�[�h�΍�j
            var task = Task.Factory.StartNew(() =>
            {
                DataTable dt = new DataTable();
                string sql = "select a.*, b.pack_id, '" + txtCarton.Text + "' as carton_id from (" +
                             "select * from t_module where tray_id in (" +
                             "select tray_id from t_tray where pack_id in (" +
                             "select pack_id from t_pack where carton_id = '" + txtCarton.Text + "'))) a " +
                             "inner join t_tray b on a.tray_id = b.tray_id";
                TfSQL tf = new TfSQL();
                System.Diagnostics.Debug.Print(sql);
                tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql, ref dt);
                ExcelClass xl = new ExcelClass();
                // 2016.08.29 FUJII �G�N�Z���ւ̏o�͂���A�f�X�N�g�b�v�b�r�u�ւ̏o�͂֕ύX
                xl.ExportToCsv(dt, System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\carton.csv");
            });
        }

        // �ꎞ�e�[�u���̑I�����ꂽ�������R�[�h���A�ꊇ����������
        private void btnDeleteSelection_Click(object sender, EventArgs e)
        {
            if (dtPack.Rows.Count <= 0) return;

            // �Z���̑I��͈͂��Q��ȏ�̏ꍇ�́A���b�Z�[�W�̕\���݂̂Ńv���V�[�W���𔲂���
            if (dgvPack.Columns.GetColumnCount(DataGridViewElementStates.Selected) >= 2)
            {
                MessageBox.Show("Please select range with only one column.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            foreach (DataGridViewCell cell in dgvPack.SelectedCells)
            {
                dtPack.Rows[cell.RowIndex].Delete();
            }
            dtPack.AcceptChanges();
            updateDataGridViews(dtPack, ref dgvPack, false);
            txtPack.Focus();
            txtPack.SelectAll();
        }

        //�p�b�N�O���b�h�r���[��̃{�^���̃N���b�N�ŁAformTrayInPack���Ăяo���A�Ή�����g���[��\������i�f���Q�[�g�Ȃ��j
        private void dgvPack_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int currentRow = int.Parse(e.RowIndex.ToString());

            if (dgvPack.Columns[e.ColumnIndex] == openPack && currentRow >= 0)
            {
                //����frmTrayInPack ���J����Ă���ꍇ�́A��������悤����
                if (TfGeneral.checkOpenFormExists("frmTrayInPack"))
                {
                    MessageBox.Show("Please close the currently open form.", "Notice",
                        MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                    return;
                }

                string packId = dgvPack["pack_id", currentRow].Value.ToString();
                string batch = dgvPack["batch", currentRow].Value.ToString();
                DateTime packDate = (DateTime)dgvPack["register_date", currentRow].Value;

                //�f���Q�[�g�Ȃ�
                frmTrayInPack fT = new frmTrayInPack();
                fT.updateControls(packId, packDate, "", txtLoginName.Text, txtLoginDept.Text, userRole, batch, false, false, true, true, 3, false);
                fT.Show();
            }
        }

        // �P�p���b�g������̃J�[�g������ύX����A�e�L�X�g�{�b�N�X�̃_�u���N���b�N����N��
        private void txtOkCount_DoubleClick(object sender, EventArgs e)
        {
            //if (userRole != "super") return;

            // ���ɓ��t�H�[�����J����Ă���ꍇ�́A�������s��Ȃ�
            if (TfGeneral.checkOpenFormExists("frmCapacity"))
            {
                MessageBox.Show("Please close or complete another form.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            frmCapacity fC = new frmCapacity();
            //�q�C�x���g���L���b�`���āA�f�[�^�O���b�h���X�V����
            fC.RefreshEvent += delegate (object sndr, EventArgs excp)
            {
                capacity = fC.returnCapacity();
                updateDataGridViews(dtPack, ref dgvPack, false);
            };

            fC.updateControls(capacity);
            fC.Show();
        }

        // ����{�^����V���[�g�J�b�g�ł̏I���������Ȃ�
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x112;
            const long SC_CLOSE = 0xF060L;
            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt64() & 0xFFF0L) == SC_CLOSE) { return; }
            base.WndProc(ref m);
        }
    }
}