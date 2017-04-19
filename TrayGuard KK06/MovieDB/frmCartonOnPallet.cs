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
    public partial class frmCartonOnPallet : Form
    {
        //�e�t�H�[��frmTray�փC�x���g������A���i�f���Q�[�g�j
        public delegate void RefreshEventHandler(object sender, EventArgs e);
        public event RefreshEventHandler RefreshEvent;

        //�f�[�^�O���b�h�r���[�p�{�^��
        DataGridViewButtonColumn openCarton;

        // �v�����g�p�e�L�X�g�t�@�C���̕ۑ��p�t�H���_���A��{�ݒ�t�@�C���Őݒ肷��
        string appconfig = System.Environment.CurrentDirectory + "\\info.ini";
        
        //���̑��A�񃍁[�J���ϐ�
        DataTable dtCarton;
        DataTable dtLot;
        bool formAddMode;
        bool formReprintMode;
        bool palletIdCanceled;
        bool palletIdInvoiced;
        string userRole;
        int okCount;
        int capacity = 48;
        string maxLot;
        DateTime registerDate;
        bool sound;
        //int position;

        // �R���X�g���N�^
        public frmCartonOnPallet()
        {
            InitializeComponent();
        }

        // ���[�h���̏���
        private void frmCartonOnPallet_Load(object sender, EventArgs e)
        {
            this.Text = this.Text + " " + Assembly.GetExecutingAssembly().GetName().Version;
            //// �v�����g�p�t�@�C���̕ۑ���t�H���_���A��{�ݒ�t�@�C���Őݒ肷��
            //directory = readIni("TARGET DIRECTORY", "DIR", appconfig);

            // ���t�H�[���̕\���ꏊ���w��
            this.Left = 250;
            this.Top = 20;
            //if (position == 2) { this.Left = 400; this.Top = 30; }
            //else if (position == 3) { this.Left = 450; this.Top = 40; }
            //else if (position == 4) { this.Left = 500; this.Top = 40; }

            // �e�폈���p�̃e�[�u���𐶐����A�f�[�^��ǂݍ���
            dtCarton = new DataTable();
            defineCartonTable(ref dtCarton);
            if (!formAddMode) readCartonInfo(ref dtCarton);

            // �O���b�g�r���[�̍X�V
            updateDataGridViews(dtCarton, ref dgvCarton, true);

            // �ǉ����[�h�A�{�����[�h�̐؂�ւ�
            changeMode();
        }

        // �T�u�v���V�[�W���F �ǉ����[�h�A�{�����[�h�̐؂�ւ�
        private void changeMode()
        {
            // �ǉ����[�h�̏ꍇ
            if (formAddMode)
            {
                txtCarton.Enabled = true;
                btnRegisterPallet.Text = "Register Pallet";
                btnRegisterPallet.Enabled = false;
                btnCancelPallet.Enabled = false;
                btnClose.Enabled = true;
                btnDeleteSelection.Enabled = true;
            }
            // �{�����[�h�̏ꍇ
            else
            {
                txtCarton.Enabled = false;
                btnRegisterPallet.Text = "Re-Print Label";
                // 2016.09.24 FUJII  �ăv�����g���[�h��ǉ��ifrmPack�ɂāA�g���[�h�c���L�[�Ƃ��āA�p�b�N�h�c����肵���ꍇ�̂݁A�Ĉ���\�j
                // 2017.03.08 FUJII  �ăv�����g���[�h��ύX�i���[�U�[�����A�Ĉ�����[�h������p�~�j
                //btnRegisterPallet.Enabled = palletIdCanceled ? false : ((formReprintMode && userRole == "super") ? true : false);
                btnRegisterPallet.Enabled = palletIdCanceled ? false : true;

                btnCancelPallet.Enabled = false;
                btnClose.Enabled = true;
                btnDeleteSelection.Enabled = false;
                if (userRole == "super")
                {
                    btnCancelPallet.Enabled = (palletIdCanceled || palletIdInvoiced) ? false : true;
                    // 2016.08.29 FUJII �{�����[�h�̏ꍇ�̂݁A�o�^�ς݃J�[�g���̒u���^�폜�^�ǉ��{�^����L���ɂ���
                    btnReplaceCarton.Enabled = (palletIdCanceled || palletIdInvoiced) ? false : true;
                    btnReplaceCarton.Visible = (palletIdCanceled || palletIdInvoiced) ? false : true;
                    btnDeleteCarton.Enabled = (palletIdCanceled || palletIdInvoiced) ? false : true;
                    btnDeleteCarton.Visible = (palletIdCanceled || palletIdInvoiced) ? false : true;
                    btnAddCarton.Enabled = (palletIdCanceled || palletIdInvoiced) ? false : true;
                    btnAddCarton.Visible = (palletIdCanceled || palletIdInvoiced) ? false : true;
                }
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
        public void updateControls(string pid, DateTime pdate, string uid, string uname, string udept, string urole, string shift, 
            bool addMode, bool returnMode, bool canceled, bool invoiced, bool reprintMode)
        {
            txtPallet.Text = pid;
            dtpRegisterDate.Value = pdate;
            txtLoginName.Text = uname;
            txtLoginDept.Text = udept;
            userRole = urole;
            formAddMode = addMode;
            palletIdCanceled = canceled;
            palletIdInvoiced = invoiced;
            formReprintMode = reprintMode;
        }

        // �T�u�v���V�[�W���F�f�[�^�e�[�u���̒�`
        private void defineCartonTable(ref DataTable dt)
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

        // �T�u�v���V�[�W���F�c�a����f�[�^�e�[�u���ւ̓ǂݍ���
        private void readCartonInfo(ref DataTable dt)
        {
            dt.Rows.Clear();
            string sql = "select carton_id, lot, m_qty, batch, register_date, rg_user, " +
                "case when l_cnt >= 2 then 'T' else 'F' end as multi_lot, " +
                "'OK' as check from t_carton where pallet_id='" + txtPallet.Text + "'";
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
            if(formAddMode) colorViewForCanceledAndInvoiced(ref dgv1);

            // �d�����R�[�h�A����тP�Z���Q�d���͂��}�[�L���O����
            if (formAddMode) colorViewForDuplicateCarton(ref dgv1);

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
                    txtCarton.Enabled = false;
                    btnRegisterPallet.Enabled = true;
                    btnDeleteSelection.Enabled = true;
                }
                else
                {
                    txtCarton.Enabled = true;
                    btnRegisterPallet.Enabled = false;
                    btnDeleteSelection.Enabled = true;
                    txtCarton.SelectAll(); // �A���X�L�����p
                }
            }

            // �o�b�`�e�L�X�g�{�b�N�X�ցA�O���b�h�r���[�̃o�b�`��\������i�����o�b�`�����̏ꍇ�́A�x������j
            if(dt1.Rows.Count >= 1) txtBatch.Text = dt1.Rows[0]["batch"].ToString();
            if (dt1.Rows.Count == 2 && dt1.Rows[0]["batch"].ToString() != dt1.Rows[dt1.Rows.Count - 1]["batch"].ToString())
                { txtBatch.Text = "Error"; soundAlarm();} 
        }

        // �T�u�T�u�v���V�[�W���F�O���b�g�r���[�E�[�Ƀ{�^����ǉ�
        private void addButtonsToDataGridView(DataGridView dgv)
        {
            openCarton = new DataGridViewButtonColumn();
            openCarton.HeaderText = string.Empty;
            openCarton.Text = "Open";
            openCarton.UseColumnTextForButtonValue = true;
            openCarton.Width = 80;
            dgv.Columns.Add(openCarton);
        }

        // �T�u�v���V�[�W���F�V���A���ԍ��d���Ȃ��̂o�`�r�r�����擾����
        private int getOkCount(DataTable dt)
        {
            DataTable distinct = dt.DefaultView.ToTable(true, new string[] { "carton_id", "check" });
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

            // �e�J�[�g���Ɋ܂܂��A�o�b�N�h�c���X�g���擾����
            TfSQL tf = new TfSQL();
            string sql1 = "select pack_id from t_pack where carton_id in (";
            string sql2 = string.Empty;
            var query1 = dt.AsEnumerable().Select(row => new { carton_id = row.Field<string>("carton_id") });
            foreach (var q in query1) sql2 += "'" + q.carton_id + "', ";
            string sql3 = sql1 + VBS.Left(sql2, sql2.Length - 2) + ")";
            System.Diagnostics.Debug.Print(sql3);
            DataTable dtTemp1 = new DataTable();
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql3, ref dtTemp1);

            // �e�p�b�N�Ɋ܂܂��A�g���[�h�c���X�g���擾����
            sql1 = "select tray_id from t_tray where pack_id in (";
            sql2 = string.Empty;
            var query2 = dtTemp1.AsEnumerable().Select(row => new { pack_id = row.Field<string>("pack_id") });
            foreach (var q in query2) sql2 += "'" + q.pack_id + "', ";
            sql3 = sql1 + VBS.Left(sql2, sql2.Length - 2) + ")";
            System.Diagnostics.Debug.Print(sql3);
            DataTable dtTemp2 = new DataTable();
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql3, ref dtTemp1);

            // ��L�Ŏ擾�����g���[�h�c�������W���[������A���b�g�W�v���쐬����
            sql1 = "select lot, count(lot) as qty from t_module where tray_id in (";
            sql2 = string.Empty;
            var query3 = dtTemp1.AsEnumerable().Select(row => new { tray_id = row.Field<string>("tray_id") });
            foreach (var q in query3) sql2 += "'" + q.tray_id + "', ";
            sql3 = sql1 + VBS.Left(sql2, sql2.Length - 2) + ") group by lot order by qty desc, lot";
            System.Diagnostics.Debug.Print(sql3);
            DataTable dtTemp3 = new DataTable();
            tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql3, ref dtTemp3);

            // ���b�g�W�v�\���e�[�u���ɁA�ꎞ�e�[�u���̏����ڂ�
            dgvLotSummary.DataSource = null;
            dgvLotSummary.Refresh();
            dtLot = new DataTable();
            var query4 = dtTemp3.AsEnumerable().Select(r => new {lot = r.Field<string>("lot"), qty = r.Field<Int64>("qty") });
            // ��̒ǉ�
            foreach (var q in query4) dtLot.Columns.Add(q.lot, typeof(int));
            dtLot.Columns.Add("total", typeof(int));
            // �s�̒ǉ�
            dtLot.Rows.Add(); 
            foreach (var q in query4) dtLot.Rows[0][q.lot] = q.qty;
            dtLot.Rows[0]["total"] = query4.Sum(a => a.qty);

            // dgvLotSummary_ControlAdded ����������A�U�T�T��葽�������舵�����߁A�v�d�h�f�g�s�ݒ���P�O�O����P�֕ύX
            dgvLotSummary.DataSource = dtLot;
            return query4.First().lot;
        }

        // �C�x���g�F ���b�g�W�v�f�[�^�O���b�h�r���[�́A�����U�T�T���A�P�O�O�{�̂U�T�T�O�O�Ƃ���
        private void dgvLotSummary_ColumnAdded(object sender, DataGridViewColumnEventArgs e)
        {
            e.Column.FillWeight = 1;
        }

        // �V���A�����X�L�������ꂽ���̏���            
        private void txtModuleId_KeyDown(object sender, KeyEventArgs e)
        {
            // �G���^�[�L�[�̏ꍇ�A�e�L�X�g�{�b�N�X�̌������P�T���̏ꍇ�̂݁A�������s��
            if (e.KeyCode != Keys.Enter || txtCarton.Text.Length != 15) return;

            string carton = txtCarton.Text;
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
            updateDataGridViews(dtCarton, ref dgvCarton, false);
        }

        // �o�^�{�^���������A�e��m�F�A�{�b�N�X�h�c�̔��s�A�V���A���̓o�^�A�o�[�R�[�h���x���̃v�����g���s��
        private void btnRegisterTray_Click(object sender, EventArgs e)
        {
            //�{�����[�h�̏ꍇ�́A�v�����g�A�E�g
            if (!formAddMode)
            {
                string pallet = txtPallet.Text;
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
                tfs.printStart("packCartonInternal", pallet, dtLot, txtBatch.Text, dtpRegisterDate.Value, "Pallet", "Pega", 2, userBin);
                tfs.printStart("packCartonPega", pallet, dtLot, txtBatch.Text, dtpRegisterDate.Value, "Pallet", "Pega", 1, userBin);
                //}
                //else
                //{
                //    tfs.printStart("packCartonInternal", pallet, dtLot, txtBatch.Text, dtpRegisterDate.Value, "Pallet", "Fox", 2, userBin);
                //}
                return;
            }

            //�����̃o�b�`���������Ă���ꍇ�́A�x������
            if (txtBatch.Text == "Error")
            {
                MessageBox.Show("You can not register 2 batches in a pallet.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // �o�^�������́A����{�^���������A���ׂẴR���g���[���𖳌��ɂ���
            txtCarton.Enabled = false;
            btnRegisterPallet.Enabled = false;
            btnDeleteSelection.Enabled = false;

            // �p���b�g�h�c�̐V�K�̔�
            // 2016.08.22 FUJII  �p���b�g�h�c�̐V�K�̔ԃv���V�[�W���ugetNewPalletId�v���A�g�����U�N�V���������o�[�W�����֕ύX
            // string palletNew = getNewPalletId(txtBatch.Text, txtLoginName.Text);
            TfSQL tf = new TfSQL();
            string palletNew = tf.sqlGetNewPalletId(maxLot, txtBatch.Text, txtLoginName.Text, dtLot, ref registerDate);
            if (palletNew == string.Empty)
            {
                MessageBox.Show("An error happened in the pallet id issuing process.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            //�p�b�N�e�[�u���̃t�B�[���h�b�`�q�s�n�m�h�c�A���̑����X�V����
            bool res = tf.sqlMultipleUpdateCartonOnPallet(dtCarton, palletNew);

            if (res)
            {
                //�o�^�ς݂̏�Ԃ�\��
                txtPallet.Text = palletNew;
                dtpRegisterDate.Value = registerDate;

                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                this.Focus();
                MessageBox.Show("Pallet ID: " + palletNew + Environment.NewLine +
                    "and its cartons were registered.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //���[�U�[�ɂ�郁�b�Z�[�W�{�b�N�X�m�F��̏���
                txtPallet.Text = String.Empty;
                txtCarton.Text = String.Empty;
                dtCarton.Clear();
                updateDataGridViews(dtCarton, ref dgvCarton, false);
            }
        }

        // �X�[�p�[���[�U�[�Ɍ���A�o�^�σJ�[�g�����L�����Z���ł���i�C���{�C�X������͕s�j
        private void btnCancelCarton_Click(object sender, EventArgs e)
        {
            // �{���ɍ폜���Ă悢���A�Q�d�Ŋm�F����B
            DialogResult result1 = MessageBox.Show("Do you really want to cancel this pallet?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result1 == DialogResult.No) return;

            DialogResult result2 = MessageBox.Show("Are you really sure? Please select NO if you are not sure.", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (result2 == DialogResult.No) return;

            // �L�����Z���̎��s
            string palletId = txtPallet.Text;
            TfSQL tf = new TfSQL();
            bool res = tf.sqlCancelCartonOnPallet(dtCarton, palletId, txtLoginName.Text);
            if (res)
            {
                //�{�t�H�[���̃f�[�^�O���b�g�r���[�X�V
                dtCarton.Clear();
                updateDataGridViews(dtCarton, ref dgvCarton, false);

                //�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                this.RefreshEvent(this, new EventArgs());
                this.Focus();
                MessageBox.Show("Pallet ID " + palletId + " was canceled.", "Process Result", MessageBoxButtons.OK, MessageBoxIcon.Information);

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

        // �T�u�v���V�[�W���F�L�����Z���ς݂܂��̓C���{�C�X�ς݂̃p�b�N���}�[�L���O����
        private void colorViewForCanceledAndInvoiced(ref DataGridView dgv)
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
        private void colorViewForDuplicateCarton(ref DataGridView dgv)
        {
            if (dgv.Rows.Count <= 0) return;

            DataTable dt = ((DataTable)dgv.DataSource).Copy();
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                string carton = dgv["carton_id", i].Value.ToString();
                DataRow[] dr = dt.Select("carton_id = '" + carton + "'");
                if (dr.Length >= 2)
                {
                    dgv["carton_id", i].Style.BackColor = Color.Red;
                    soundAlarm();
                }
                else
                {
                    dgv["carton_id", i].Style.BackColor = Color.FromKnownColor(KnownColor.Window);
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
            dt = (DataTable)dgvCarton.DataSource;
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
                string sql = "select a.*, b.pack_id, c.carton_id, '" + txtPallet.Text + "' as pallet_id from (" +
                             "select * from t_module where tray_id in (" +
                             "select tray_id from t_tray where pack_id in (" +
                             "select pack_id from t_pack where carton_id in (" +
                             "select carton_id from t_carton where pallet_id = '" + txtPallet.Text + "')))) a " +
                             "inner join t_tray b on a.tray_id = b.tray_id " +
                             "inner join t_pack c on b.pack_id = c.pack_id";
                TfSQL tf = new TfSQL();
                System.Diagnostics.Debug.Print(sql);
                tf.sqlDataAdapterFillDatatableFromTrayGuardDb(sql, ref dt);
                ExcelClass xl = new ExcelClass();
                //xl.ExportToExcel(dt);
                // 2016.08.29 FUJII �G�N�Z���ւ̏o�͂���A�f�X�N�g�b�v�b�r�u�ւ̏o�͂֕ύX
                xl.ExportToCsv(dt, System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\pallet.csv");
            });
        }

        // �ꎞ�e�[�u���̑I�����ꂽ�������R�[�h���A�ꊇ����������
        private void btnDeleteSelection_Click(object sender, EventArgs e)
        {
            if (dtCarton.Rows.Count <= 0) return;

            // �Z���̑I��͈͂��Q��ȏ�̏ꍇ�́A���b�Z�[�W�̕\���݂̂Ńv���V�[�W���𔲂���
            if (dgvCarton.Columns.GetColumnCount(DataGridViewElementStates.Selected) >= 2)
            {
                MessageBox.Show("Please select range with only one column.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            foreach (DataGridViewCell cell in dgvCarton.SelectedCells)
            {
                dtCarton.Rows[cell.RowIndex].Delete();
            }
            dtCarton.AcceptChanges();
            updateDataGridViews(dtCarton, ref dgvCarton, false);
            txtCarton.Focus();
            txtCarton.SelectAll();
        }

        //�p�b�N�O���b�h�r���[��̃{�^���̃N���b�N�ŁAformTrayInPack���Ăяo���A�Ή�����g���[��\������i�f���Q�[�g�Ȃ��j
        private void dgvPack_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int currentRow = int.Parse(e.RowIndex.ToString());

            if (dgvCarton.Columns[e.ColumnIndex] == openCarton && currentRow >= 0)
            {
                //����frmPackInCarton ���J����Ă���ꍇ�́A��������悤����
                if (TfGeneral.checkOpenFormExists("frmPackInCarton"))
                {
                    MessageBox.Show("Please close the currently open form.", "Notice",
                        MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                    return;
                }

                string packId = dgvCarton["carton_id", currentRow].Value.ToString();
                string batch = dgvCarton["batch", currentRow].Value.ToString();
                DateTime packDate = (DateTime)dgvCarton["register_date", currentRow].Value;

                //�f���Q�[�g�Ȃ�
                frmPackInCarton fT = new frmPackInCarton();
                fT.updateControls(packId, packDate, "", txtLoginName.Text, txtLoginDept.Text, userRole, batch, false, false, true, true, 2, false);
                fT.Show();
            }
        }

        // �P�J�[�g��������̃p�b�N����ύX����A�e�L�X�g�{�b�N�X�̃_�u���N���b�N����N��
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
                updateDataGridViews(dtCarton, ref dgvCarton, false);
            };

            fC.updateControls(capacity);
            fC.Show();
        }

        // 2016.08.29 FUJII �@�\�ǉ�
        // �X�[�p�[���[�U�[�Ɍ���A�o�^�ς݂̃J�[�g���̒u���������ł���
        private void btnReplaceCarton_Click(object sender, EventArgs e)
        {
            adjustAlreadyRegisteredPallet("replace");
        }

        // �X�[�p�[���[�U�[�Ɍ���A�o�^�ς݂̃J�[�g���̍폜���ł���
        private void btnDeleteCarton_Click(object sender, EventArgs e)
        {
            adjustAlreadyRegisteredPallet("delete");
        }

        // �X�[�p�[���[�U�[�Ɍ���A�o�^�ς݃p���b�g�ɑ΂��A�J�[�g���̒ǉ����ł���
        private void btnAddCarton_Click(object sender, EventArgs e)
        {
            adjustAlreadyRegisteredPallet("add");
        }

        // �T�u�v���V�[�W���F�o�^�σp���b�g�ɑ΂��鏈���ɂ��āA�u���E�폜�E�ǉ��ŁA���ʂ��镔��
        private void adjustAlreadyRegisteredPallet(string modePalletAdjust)
        {
            if (dtCarton.Rows.Count <= 0) return;

            if (modePalletAdjust== "replace" || modePalletAdjust== "delete")
            {
                // �Z���̑I��͈͂��Q��ȏ�̏ꍇ�́A���b�Z�[�W�̕\���݂̂Ńv���V�[�W���𔲂���
                if (dgvCarton.Columns.GetColumnCount(DataGridViewElementStates.Selected) >= 2 ||
                        dgvCarton.Rows.GetRowCount(DataGridViewElementStates.Selected) >= 2 ||
                        dgvCarton.CurrentCell.ColumnIndex != 0)
                {
                    MessageBox.Show("Please select only one carton id.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                    return;
                }
            }

            // �u�������p�t�H�[�������ɊJ���Ă��Ȃ����A�m�F����
            if (TfGeneral.checkOpenFormExists("frmCartonReplace"))
            {
                MessageBox.Show("Please close or complete another form.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                return;
            }

            string curCarton = modePalletAdjust == "add" ? string.Empty : dgvCarton.CurrentCell.Value.ToString();
            int curRowIndex = modePalletAdjust == "add" ? dgvCarton.Rows.Count : dgvCarton.CurrentRow.Index;
            frmCartonAdjust fR = new frmCartonAdjust();

            //�u�������t�H�[���C�x���g���L���b�`���āA���t�H�[���f�[�^�O���b�h���X�V���A�p���b�g�t�H�[�����X�V����
            fR.RefreshEvent += delegate (object sndr, EventArgs excp)
            {
                //���W���[���e�[�u���E�f�[�^�O���b�h�̍X�V
                readCartonInfo(ref dtCarton);
                updateDataGridViews(dtCarton, ref dgvCarton, false);
                //�g���[�e�[�u���̍X�V�A����ɁA�e�t�H�[��frmTray�̃f�[�^�O���b�g�r���[���X�V���邽�߁A�f���Q�[�g�C�x���g�𔭐�������
                updatePalletId(txtPallet.Text, txtLoginName.Text);
                this.RefreshEvent(this, new EventArgs());
                this.Focus();
            };

            fR.updateControls(txtPallet.Text, curCarton, curRowIndex + 1, txtLoginName.Text, modePalletAdjust);
            fR.Show();
        }
        
        // �T�u�v���V�[�W���F�p���b�g���̍X�V
        private bool updatePalletId(string PalletId, string uname)
        {
            // ���b�g���ʁA�X�V���A�X�V���[�U�[�ɂ��čX�V����
            string lot = dtLot.Columns[0].ColumnName;
            int count = dtLot.Columns.Count - 1;
            int m_qty = (int)dtLot.Rows[0]["total"];
            string sql = "update t_pallet set " +
                "lot ='" + lot + "', " +
                "l_cnt = " + count + ", " +
                "m_qty = " + m_qty + ", " +
                "batch = '" + txtBatch.Text + "', " +
                "register_date ='" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "', " +
                "rg_user ='" + uname + "' " +
                "where pallet_id ='" + PalletId + "'";

            System.Diagnostics.Debug.Print(sql);
            TfSQL tf = new TfSQL();
            bool res = tf.sqlExecuteNonQuery(sql, false);
            return res;
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
    }
}