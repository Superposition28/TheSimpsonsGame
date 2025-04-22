import os

if os.path.exists('Modules'):
	if os.path.exists('Modules\\QBMS_TSG'):
		import Modules.QBMS_TSG.run as run_qbms
		import Modules.QBMS_TSG.init as init


	init.main()
	run_qbms.main()