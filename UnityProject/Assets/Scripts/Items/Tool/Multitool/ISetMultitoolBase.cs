﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISetMultitoolBase
{
	MultitoolConnectionType ConType { get; }
}


public interface ISetMultitoolSlave : ISetMultitoolBase
{
	void SetMaster(ISetMultitoolMaster iMaster);
}

public interface ISetMultitoolSlaveMultiMaster : ISetMultitoolBase
{
	void SetMasters(List<ISetMultitoolMaster> iMasters);
}

public interface ISetMultitoolMaster : ISetMultitoolBase
{
	bool MultiMaster { get; }
	void AddSlave(object slaveObject);
}
