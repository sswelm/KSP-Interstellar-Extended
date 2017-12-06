namespace FNPlugin.Extensions
{
	public class AtmosphericFloatCurves
	{
		public FloatCurve MassDensityAtmosphereCubeCm { get; private set; }
		public FloatCurve ParticlesAtmosphereCurbeM { get; private set; }
		public FloatCurve ParticlesHydrogenCubeM { get; private set; }
		public FloatCurve HydrogenIonsCubeCm { get; private set; }

		private static AtmosphericFloatCurves _instance;

		public static AtmosphericFloatCurves Instance
		{
			get { return _instance ?? (_instance = new AtmosphericFloatCurves()); }
		}

		private AtmosphericFloatCurves()
		{
			InitializeAtmosphereParticles();
		}

		private void InitializeAtmosphereParticles()
		{
			InitializeDensityAtmosphereCubeCm();

			InitializeParticlesAtmosphereCurbeM();

			InitializeParticlesHydrogenCubeM();

			InitializeHydrogenIonsCubeCm();
		}

		private void InitializeHydrogenIonsCubeCm()
		{
			if (HydrogenIonsCubeCm != null) return;

			HydrogenIonsCubeCm = new FloatCurve();
			HydrogenIonsCubeCm.Add(0, 0);
			HydrogenIonsCubeCm.Add(284, 0);
			HydrogenIonsCubeCm.Add(285, 1.00e+8f);
			HydrogenIonsCubeCm.Add(299, 2.49e+8f);
			HydrogenIonsCubeCm.Add(312, 4.40e+8f);
			HydrogenIonsCubeCm.Add(334, 6.34e+8f);
			HydrogenIonsCubeCm.Add(359, 8.83e+8f);
			HydrogenIonsCubeCm.Add(377, 1.11e+9f);
			HydrogenIonsCubeCm.Add(396, 1.26e+9f);
			HydrogenIonsCubeCm.Add(411, 1.36e+9f);
			HydrogenIonsCubeCm.Add(437, 1.51e+9f);
			HydrogenIonsCubeCm.Add(464, 1.65e+9f);
			HydrogenIonsCubeCm.Add(504, 1.78e+9f);
			HydrogenIonsCubeCm.Add(536, 1.86e+9f);
			HydrogenIonsCubeCm.Add(588, 2.00e+9f);
			HydrogenIonsCubeCm.Add(641, 2.20e+9f);
			HydrogenIonsCubeCm.Add(678, 2.40e+9f);
			HydrogenIonsCubeCm.Add(715, 2.64e+9f);
			HydrogenIonsCubeCm.Add(754, 2.94e+9f);
			HydrogenIonsCubeCm.Add(793, 3.33e+9f);
			HydrogenIonsCubeCm.Add(829, 3.76e+9f);
			HydrogenIonsCubeCm.Add(864, 4.26e+9f);
			HydrogenIonsCubeCm.Add(904, 4.97e+9f);
			HydrogenIonsCubeCm.Add(939, 5.72e+9f);
			HydrogenIonsCubeCm.Add(968, 6.43e+9f);
			HydrogenIonsCubeCm.Add(992, 7.07e+9f);
			HydrogenIonsCubeCm.Add(1010, 7.61e+9f);
			HydrogenIonsCubeCm.Add(1040, 7.95e+9f);
			HydrogenIonsCubeCm.Add(1070, 8.15e+9f);
			HydrogenIonsCubeCm.Add(1100, 8.13e+9f);
			HydrogenIonsCubeCm.Add(1140, 7.89e+9f);
			HydrogenIonsCubeCm.Add(1170, 7.57e+9f);
			HydrogenIonsCubeCm.Add(1190, 7.21e+9f);
			HydrogenIonsCubeCm.Add(1240, 6.46e+9f);
			HydrogenIonsCubeCm.Add(2000, 3.8e+7f);
			HydrogenIonsCubeCm.Add(4000, 1.9e+5f);
			HydrogenIonsCubeCm.Add(8000, 9.5e+2f);
			HydrogenIonsCubeCm.Add(10000, 0);
		}

		private void InitializeParticlesHydrogenCubeM()
		{
			if (ParticlesHydrogenCubeM != null) return;

			ParticlesHydrogenCubeM = new FloatCurve();
			ParticlesHydrogenCubeM.Add(0, 0);
			ParticlesHydrogenCubeM.Add(72.5f, 1.747e+9f);
			ParticlesHydrogenCubeM.Add(73.0f, 2.872e+9f);
			ParticlesHydrogenCubeM.Add(73.5f, 5.154e+9f);
			ParticlesHydrogenCubeM.Add(74.0f, 1.009e+10f);
			ParticlesHydrogenCubeM.Add(74.5f, 2.138e+10f);
			ParticlesHydrogenCubeM.Add(75.0f, 4.836e+10f);
			ParticlesHydrogenCubeM.Add(75.5f, 1.144e+11f);
			ParticlesHydrogenCubeM.Add(76.0f, 2.760e+11f);
			ParticlesHydrogenCubeM.Add(76.5f, 6.612e+11f);
			ParticlesHydrogenCubeM.Add(77.0f, 1.531e+12f);
			ParticlesHydrogenCubeM.Add(77.5f, 3.351e+12f);
			ParticlesHydrogenCubeM.Add(78.0f, 6.813e+12f);
			ParticlesHydrogenCubeM.Add(78.5f, 1.274e+13f);
			ParticlesHydrogenCubeM.Add(79.0f, 2.180e+13f);
			ParticlesHydrogenCubeM.Add(79.5f, 3.420e+13f);
			ParticlesHydrogenCubeM.Add(80.0f, 4.945e+13f);
			ParticlesHydrogenCubeM.Add(80.5f, 6.637e+13f);
			ParticlesHydrogenCubeM.Add(81.0f, 8.346e+13f);
			ParticlesHydrogenCubeM.Add(81.5f, 9.920e+13f);
			ParticlesHydrogenCubeM.Add(82.0f, 1.124e+14f);
			ParticlesHydrogenCubeM.Add(82.5f, 1.225e+14f);
			ParticlesHydrogenCubeM.Add(83.0f, 1.292e+14f);
			ParticlesHydrogenCubeM.Add(83.5f, 1.328e+14f);
			ParticlesHydrogenCubeM.Add(84.0f, 1.335e+14f);
			ParticlesHydrogenCubeM.Add(84.5f, 1.320e+14f);
			ParticlesHydrogenCubeM.Add(85.0f, 1.287e+14f);
			ParticlesHydrogenCubeM.Add(85.5f, 1.241e+14f);
			ParticlesHydrogenCubeM.Add(86.0f, 1.187e+14f);
			ParticlesHydrogenCubeM.Add(87, 1.066e+13f);
			ParticlesHydrogenCubeM.Add(88, 9.426e+13f);
			ParticlesHydrogenCubeM.Add(89, 8.257e+13f);
			ParticlesHydrogenCubeM.Add(90, 7.203e+13f);
			ParticlesHydrogenCubeM.Add(91, 6.276e+13f);
			ParticlesHydrogenCubeM.Add(92, 5.474e+13f);
			ParticlesHydrogenCubeM.Add(93, 4.786e+13f);
			ParticlesHydrogenCubeM.Add(94, 4.198e+13f);
			ParticlesHydrogenCubeM.Add(95, 3.698e+13f);
			ParticlesHydrogenCubeM.Add(96, 3.272e+13f);
			ParticlesHydrogenCubeM.Add(97, 2.909e+13f);
			ParticlesHydrogenCubeM.Add(98, 2.598e+13f);
			ParticlesHydrogenCubeM.Add(99, 2.332e+13f);
			ParticlesHydrogenCubeM.Add(100, 2.101e+13f);
			ParticlesHydrogenCubeM.Add(101, 1.901e+13f);
			ParticlesHydrogenCubeM.Add(102, 1.726e+13f);
			ParticlesHydrogenCubeM.Add(103, 1.572e+13f);
			ParticlesHydrogenCubeM.Add(104, 1.435e+13f);
			ParticlesHydrogenCubeM.Add(105, 1.313e+13f);
			ParticlesHydrogenCubeM.Add(106, 1.203e+13f);
			ParticlesHydrogenCubeM.Add(107, 1.104e+13f);
			ParticlesHydrogenCubeM.Add(108, 1.013e+13f);
			ParticlesHydrogenCubeM.Add(109, 9.299e+13f);
			ParticlesHydrogenCubeM.Add(110, 8.534e+12f);
			ParticlesHydrogenCubeM.Add(111, 7.827e+12f);
			ParticlesHydrogenCubeM.Add(112, 7.173e+12f);
			ParticlesHydrogenCubeM.Add(113, 6.569e+12f);
			ParticlesHydrogenCubeM.Add(114, 6.012e+12f);
			ParticlesHydrogenCubeM.Add(115, 5.500e+12f);
			ParticlesHydrogenCubeM.Add(120, 3.551e+12f);
			ParticlesHydrogenCubeM.Add(125, 2.477e+12f);
			ParticlesHydrogenCubeM.Add(130, 1.805e+12f);
			ParticlesHydrogenCubeM.Add(140, 1.029e+12f);
			ParticlesHydrogenCubeM.Add(150, 6.468e+11f);
			ParticlesHydrogenCubeM.Add(160, 4.485e+11f);
			ParticlesHydrogenCubeM.Add(170, 3.400e+11f);
			ParticlesHydrogenCubeM.Add(180, 2.774e+11f);
			ParticlesHydrogenCubeM.Add(190, 2.394e+11f);
			ParticlesHydrogenCubeM.Add(200, 2.154e+11f);
			ParticlesHydrogenCubeM.Add(210, 1.995e+11f);
			ParticlesHydrogenCubeM.Add(220, 1.887e+11f);
			ParticlesHydrogenCubeM.Add(230, 1.809e+11f);
			ParticlesHydrogenCubeM.Add(240, 1.752e+11f);
			ParticlesHydrogenCubeM.Add(250, 1.707e+11f);
			ParticlesHydrogenCubeM.Add(300, 1.569e+11f);
			ParticlesHydrogenCubeM.Add(350, 1.477e+11f);
			ParticlesHydrogenCubeM.Add(400, 1.399e+11f);
			ParticlesHydrogenCubeM.Add(450, 1.327e+11f);
			ParticlesHydrogenCubeM.Add(500, 1.260e+11f);
			ParticlesHydrogenCubeM.Add(550, 1.198e+11f);
			ParticlesHydrogenCubeM.Add(600, 1.139e+11f);
			ParticlesHydrogenCubeM.Add(650, 1.085e+11f);
			ParticlesHydrogenCubeM.Add(700, 1.033e+11f);
			ParticlesHydrogenCubeM.Add(750, 9.848e+10f);
			ParticlesHydrogenCubeM.Add(800, 9.393e+10f);
			ParticlesHydrogenCubeM.Add(850, 8.965e+10f);
			ParticlesHydrogenCubeM.Add(900, 8.562e+10f);
			ParticlesHydrogenCubeM.Add(950, 8.182e+10f);
			ParticlesHydrogenCubeM.Add(1000, 7.824e+10f);
			ParticlesHydrogenCubeM.Add(2000, 3.912e+8f);
			ParticlesHydrogenCubeM.Add(4000, 1.956e+6f);
			ParticlesHydrogenCubeM.Add(8000, 9.780e+3f);
			ParticlesHydrogenCubeM.Add(10000, 0);
		}

		private void InitializeParticlesAtmosphereCurbeM()
		{
			if (ParticlesAtmosphereCurbeM != null) return;

			ParticlesAtmosphereCurbeM = new FloatCurve();
			ParticlesAtmosphereCurbeM.Add(0, 2.55e+25f);
			ParticlesAtmosphereCurbeM.Add(2, 2.09e+25f);
			ParticlesAtmosphereCurbeM.Add(4, 1.70e+25f);
			ParticlesAtmosphereCurbeM.Add(6, 1.37e+25f);
			ParticlesAtmosphereCurbeM.Add(8, 1.09e+25f);
			ParticlesAtmosphereCurbeM.Add(10, 8.60e+24f);
			ParticlesAtmosphereCurbeM.Add(12, 6.49e+24f);
			ParticlesAtmosphereCurbeM.Add(14, 4.74e+24f);
			ParticlesAtmosphereCurbeM.Add(16, 3.46e+24f);
			ParticlesAtmosphereCurbeM.Add(18, 2.53e+24f);
			ParticlesAtmosphereCurbeM.Add(20, 1.85e+24f);
			ParticlesAtmosphereCurbeM.Add(22, 1.34e+24f);
			ParticlesAtmosphereCurbeM.Add(24, 9.76e+23f);
			ParticlesAtmosphereCurbeM.Add(26, 7.12e+23f);
			ParticlesAtmosphereCurbeM.Add(28, 5.21e+23f);
			ParticlesAtmosphereCurbeM.Add(30, 3.83e+23f);
			ParticlesAtmosphereCurbeM.Add(32, 2.81e+23f);
			ParticlesAtmosphereCurbeM.Add(34, 2.06e+23f);
			ParticlesAtmosphereCurbeM.Add(36, 1.51e+23f);
			ParticlesAtmosphereCurbeM.Add(38, 1.12e+23f);
			ParticlesAtmosphereCurbeM.Add(40, 8.31e+22f);
			ParticlesAtmosphereCurbeM.Add(42, 6.23e+22f);
			ParticlesAtmosphereCurbeM.Add(44, 4.70e+22f);
			ParticlesAtmosphereCurbeM.Add(46, 3.56e+22f);
			ParticlesAtmosphereCurbeM.Add(48, 2.74e+22f);
			ParticlesAtmosphereCurbeM.Add(50, 2.14e+22f);
			ParticlesAtmosphereCurbeM.Add(52, 1.68e+22f);
			ParticlesAtmosphereCurbeM.Add(54, 1.33e+22f);
			ParticlesAtmosphereCurbeM.Add(56, 1.05e+22f);
			ParticlesAtmosphereCurbeM.Add(58, 8.24e+21f);
			ParticlesAtmosphereCurbeM.Add(60, 6.44e+21f);
			ParticlesAtmosphereCurbeM.Add(65, 3.39e+21f);
			ParticlesAtmosphereCurbeM.Add(70, 1.72e+21f);
			ParticlesAtmosphereCurbeM.Add(75, 8.30e+20f);
			ParticlesAtmosphereCurbeM.Add(80, 3.84e+20f);
			ParticlesAtmosphereCurbeM.Add(85, 1.71e+20f);
			ParticlesAtmosphereCurbeM.Add(90, 7.12e+19f);
			ParticlesAtmosphereCurbeM.Add(95, 2.92e+19f);
			ParticlesAtmosphereCurbeM.Add(100, 1.19e+19f);
			ParticlesAtmosphereCurbeM.Add(110, 2.45e+18f);
			ParticlesAtmosphereCurbeM.Add(120, 5.11e+17f);
			ParticlesAtmosphereCurbeM.Add(140, 9.32e+16f);
			ParticlesAtmosphereCurbeM.Add(160, 3.16e+16f);
			ParticlesAtmosphereCurbeM.Add(180, 1.40e+16f);
			ParticlesAtmosphereCurbeM.Add(200, 7.18e+15f);
			ParticlesAtmosphereCurbeM.Add(300, 6.51e+14f);
			ParticlesAtmosphereCurbeM.Add(400, 9.13e+13f);
			ParticlesAtmosphereCurbeM.Add(500, 2.19e+13f);
			ParticlesAtmosphereCurbeM.Add(600, 4.89e+12f);
			ParticlesAtmosphereCurbeM.Add(700, 1.14e+12f);
			ParticlesAtmosphereCurbeM.Add(800, 5.86e+11f);
			ParticlesAtmosphereCurbeM.Add(100, 1.19e+19f);
			ParticlesAtmosphereCurbeM.Add(1000, 2.06e+11f);
			ParticlesAtmosphereCurbeM.Add(2000, 1.03e+9f);
			ParticlesAtmosphereCurbeM.Add(4000, 5.2e+6f);
			ParticlesAtmosphereCurbeM.Add(8000, 2.6e+4f);
			ParticlesAtmosphereCurbeM.Add(10000, 0);
		}

		private void InitializeDensityAtmosphereCubeCm()
		{
			if (MassDensityAtmosphereCubeCm != null) return;

			MassDensityAtmosphereCubeCm = new FloatCurve();
			MassDensityAtmosphereCubeCm.Add(000, 1.340E-03f);
			MassDensityAtmosphereCubeCm.Add(001, 1.195E-03f);
			MassDensityAtmosphereCubeCm.Add(002, 1.072E-03f);
			MassDensityAtmosphereCubeCm.Add(003, 9.649E-04f);
			MassDensityAtmosphereCubeCm.Add(004, 8.681E-04f);
			MassDensityAtmosphereCubeCm.Add(005, 7.790E-04f);
			MassDensityAtmosphereCubeCm.Add(006, 6.959E-04f);
			MassDensityAtmosphereCubeCm.Add(007, 6.179E-04f);
			MassDensityAtmosphereCubeCm.Add(008, 5.446E-04f);
			MassDensityAtmosphereCubeCm.Add(009, 4.762E-04f);
			MassDensityAtmosphereCubeCm.Add(010, 4.128E-04f);
			MassDensityAtmosphereCubeCm.Add(012, 3.035E-04f);
			MassDensityAtmosphereCubeCm.Add(014, 2.203E-04f);
			MassDensityAtmosphereCubeCm.Add(016, 1.605E-04f);
			MassDensityAtmosphereCubeCm.Add(018, 1.175E-04f);
			MassDensityAtmosphereCubeCm.Add(020, 8.573E-05f);
			MassDensityAtmosphereCubeCm.Add(030, 1.611E-05f);
			MassDensityAtmosphereCubeCm.Add(040, 3.262E-06f);
			MassDensityAtmosphereCubeCm.Add(050, 8.602E-07f);
			MassDensityAtmosphereCubeCm.Add(060, 2.394E-07f);
			MassDensityAtmosphereCubeCm.Add(070, 6.017E-08f);
			MassDensityAtmosphereCubeCm.Add(080, 1.439E-08f);
			MassDensityAtmosphereCubeCm.Add(090, 3.080E-09f);
			MassDensityAtmosphereCubeCm.Add(100, 5.357E-10f);
			MassDensityAtmosphereCubeCm.Add(110, 8.711E-11f);
			MassDensityAtmosphereCubeCm.Add(120, 1.844E-11f);
			MassDensityAtmosphereCubeCm.Add(130, 7.383E-12f);
			MassDensityAtmosphereCubeCm.Add(140, 3.781E-12f);
			MassDensityAtmosphereCubeCm.Add(150, 2.185E-12f);
			MassDensityAtmosphereCubeCm.Add(160, 1.364E-12f);
			MassDensityAtmosphereCubeCm.Add(170, 8.974E-13f);
			MassDensityAtmosphereCubeCm.Add(180, 6.145E-13f);
			MassDensityAtmosphereCubeCm.Add(190, 4.333E-13f);
			MassDensityAtmosphereCubeCm.Add(200, 3.127E-13f);
			MassDensityAtmosphereCubeCm.Add(210, 2.300E-13f);
			MassDensityAtmosphereCubeCm.Add(220, 1.718E-13f);
			MassDensityAtmosphereCubeCm.Add(230, 1.300E-13f);
			MassDensityAtmosphereCubeCm.Add(240, 9.954E-14f);
			MassDensityAtmosphereCubeCm.Add(250, 7.698E-14f);
			MassDensityAtmosphereCubeCm.Add(260, 6.007E-14f);
			MassDensityAtmosphereCubeCm.Add(270, 4.725E-14f);
			MassDensityAtmosphereCubeCm.Add(280, 3.744E-14f);
			MassDensityAtmosphereCubeCm.Add(290, 2.987E-14f);
			MassDensityAtmosphereCubeCm.Add(300, 2.397E-14f);
			MassDensityAtmosphereCubeCm.Add(310, 1.934E-14f);
			MassDensityAtmosphereCubeCm.Add(320, 1.569E-14f);
			MassDensityAtmosphereCubeCm.Add(330, 1.278E-14f);
			MassDensityAtmosphereCubeCm.Add(340, 1.046E-14f);
			MassDensityAtmosphereCubeCm.Add(350, 8.594E-15f);
			MassDensityAtmosphereCubeCm.Add(400, 3.377E-15f);
			MassDensityAtmosphereCubeCm.Add(450, 1.412E-15f);
			MassDensityAtmosphereCubeCm.Add(500, 6.205E-16f);
			MassDensityAtmosphereCubeCm.Add(550, 2.854E-16f);
			MassDensityAtmosphereCubeCm.Add(600, 1.385E-16f);
			MassDensityAtmosphereCubeCm.Add(650, 7.176E-17f);
			MassDensityAtmosphereCubeCm.Add(700, 4.031E-17f);
			MassDensityAtmosphereCubeCm.Add(750, 2.477E-17f);
			MassDensityAtmosphereCubeCm.Add(800, 1.660E-17f);
			MassDensityAtmosphereCubeCm.Add(850, 1.197E-17f);
			MassDensityAtmosphereCubeCm.Add(900, 9.114E-18f);
			MassDensityAtmosphereCubeCm.Add(950, 7.211E-18f);
			MassDensityAtmosphereCubeCm.Add(1000, 5.849E-18f);
			MassDensityAtmosphereCubeCm.Add(2000, 2.9245E-20f);
			MassDensityAtmosphereCubeCm.Add(4000, 1.46225E-22f);
			MassDensityAtmosphereCubeCm.Add(8000, 7.31125E-25f);
			MassDensityAtmosphereCubeCm.Add(10000, 0);
		}
	}
}
