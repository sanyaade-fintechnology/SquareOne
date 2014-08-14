using System;
using System.Collections.Generic;
using System.IO;

using Sq1.Core.DataTypes;

namespace Sq1.Core.Repositories {
	public class RepositoryBarsSameScaleInterval {
		public string DataSourceAbspath { get; protected set; }
		public string Extension { get; protected set; }
		public BarScaleInterval ScaleInterval { get; protected set; }
		
		public List<string> SymbolsInFolder {
			get {
				List<string> ret = new List<string>();
				string[] files = Directory.GetFiles(this.DataSourceAbspath);
				for (int i = 0; i < files.Length; i++) {
					string fileName = files[i];
					string symbol = Path.GetFileNameWithoutExtension(fileName);
					ret.Add(symbol);
				}
	//			string[] folders = Directory.GetDirectories(this.RootFolder);
	//			for (int j = 0; j < folders.Length; j++) {
	//				string subFolder = folders[j];
	//				ret.AddRange(this.GetExistingSymbols(subFolder));
	//			}
				return ret;
			}
		}
		public List<BarScaleInterval> BarScalesInParentFolder {
			get {
				List<BarScaleInterval> ret = new List<BarScaleInterval>();
				string[] directories = Directory.GetDirectories(this.DataSourceAbspath);
				foreach (string folderAbsname in directories) {
					// \Data-debug\MockStaticProvider\Mock-debug\Minute-1\RIH3_Minute-1.BAR
					string folder = Path.GetFileName(folderAbsname);
					if (folder.Contains("-")) {		// "Minute-1"
						string[] intervalScale = folder.Split(new char[] { '-' });
						BarScale scaleHourMinuteSecondTick = (BarScale)Enum.Parse(typeof(BarScale), intervalScale[0], true);
						int barInterval = int.Parse(intervalScale[1]);
						ret.Add(new BarScaleInterval(scaleHourMinuteSecondTick, barInterval));
					} else {
						BarScale scaleDailyWeeklyMonthlyYearly = (BarScale)Enum.Parse(typeof(BarScale), folder, true);
						ret.Add(new BarScaleInterval(scaleDailyWeeklyMonthlyYearly, 0));
					}
				}
				return ret;
			}
		}
		
		public string SubfolderScaleInterval {
			get {
				string ret = "NO_SUBFOLDER_FOR_UNKNOWN_BARSCALE";
				switch (this.ScaleInterval.Scale) {
					case BarScale.Yearly:		ret = "Yearly";										break;
					case BarScale.Quarterly:	ret = "Quarterly";									break;
					case BarScale.Monthly:		ret = "Monthly";									break;
					case BarScale.Weekly:		ret = "Weekly";										break;
					case BarScale.Daily:		ret = "Daily";										break;
					case BarScale.Minute:		ret = "Minute-" + this.ScaleInterval.Interval;		break;
					case BarScale.Second:		ret = "Second-" + this.ScaleInterval.Interval;		break;
					case BarScale.Tick:			ret = "Tick-"   + this.ScaleInterval.Interval;		break;
				}
				return ret;
			}
		}
		
		bool createNonExistingSubfolder;
		public string SubfolderAbspath {
			get {
				string ret = Path.Combine(this.DataSourceAbspath, this.SubfolderScaleInterval);
				if (!Directory.Exists(ret) && this.createNonExistingSubfolder) {
					Directory.CreateDirectory(ret);
				}
				return ret;
			}
		}
		
		public RepositoryBarsSameScaleInterval(string dataSourceAbspath, BarScaleInterval scaleInterval, bool createNonExistingSubfolder = true, string extension = "BAR") {
			this.DataSourceAbspath = dataSourceAbspath;
			//if (this.FolderWithSymbolFiles.EndsWith(Path.DirectorySeparatorChar) == false) this.FolderWithSymbolFiles += Path.DirectorySeparatorChar;
			if (Directory.Exists(this.DataSourceAbspath) == false) Directory.CreateDirectory(this.DataSourceAbspath);
			this.Extension = extension;
			this.ScaleInterval = scaleInterval;
			this.createNonExistingSubfolder = createNonExistingSubfolder;
		}
		public bool DataFileExistsForSymbol(string symbol) {
			string symbolAbspath = this.AbspathForSymbol(symbol, false);
			return File.Exists(symbolAbspath);
		}
		public string AbspathForSymbol(string symbol, bool throwIfDoesntExist = false, bool createIfDoesntExist = false) {
			if (String.IsNullOrEmpty(symbol)) throw new ArgumentException("Symbol must not be blank");
			string fileAbspath = Path.Combine(this.SubfolderAbspath, this.FileNameForSymbol(symbol));
			if (File.Exists(fileAbspath) == false && createIfDoesntExist) {
				Directory.CreateDirectory(fileAbspath);
			}
			if (File.Exists(fileAbspath) == false && throwIfDoesntExist) {
				string msg = "AbspathForSymbol(" + symbol + "): File.Exists(" + fileAbspath + ")=false";
				throw new Exception(msg);
			}
			return fileAbspath;
		}
		public string FileNameForSymbol(string symbol) {
			symbol = symbol.ToUpper();
			// Data-debug\MockStaticProvider\Mock-debug\Minute-1\RIM3_Minute-1.BAR
			return symbol + "_" + this.SubfolderScaleInterval + "." + this.Extension;
		}

		public void SymbolDataFileAdd(string symbolToAdd, bool overwriteIfExistsDontThrow = false) {
			string abspath = this.AbspathForSymbol(symbolToAdd);
			if (File.Exists(abspath)) {
				throw new Exception("FILE_ALREADY_EXISTS[" + abspath + "]" + " SymbolDataFileAdd(" + symbolToAdd + ")");
			}
			var fs = File.Create(abspath);
			fs.Close();
		}
		public void SymbolDataFileRename(string oldSymbolName, string newSymbolName) {
			string msig = " SymbolDataFileRename(" + oldSymbolName + "=>" + newSymbolName + ")";
			string abspathOldSymbolName = this.AbspathForSymbol(oldSymbolName);
			if (File.Exists(abspathOldSymbolName) == false) {
				throw new Exception("oldSymbolName_FILE_DOESNT_EXIST[" + abspathOldSymbolName + "]" + msig);
			}
			string abspathNewSymbolName = this.AbspathForSymbol(newSymbolName);
			if (File.Exists(abspathNewSymbolName)) {
				throw new Exception("newSymbolName_FILE_ALREADY_EXISTS[" + abspathNewSymbolName + "]" + msig);
			}
			File.Move(abspathOldSymbolName, abspathNewSymbolName);
		}
		public void SymbolDataFileDelete(string symbolToDelete) {
			string abspath = this.AbspathForSymbol(symbolToDelete);
			if (File.Exists(abspath) == false) {
				throw new Exception("FILE_ALREADY_DELETED[" + abspath + "]" + " SymbolDataFileDelete(" + symbolToDelete + ")");
			}
			File.Delete(abspath);
		}
		public int DeleteAllDataFilesAllSymbols() {
			int ret = 0;
			foreach (string symbolToDelete in this.SymbolsInFolder) {
				this.SymbolDataFileDelete(symbolToDelete);
			}
			this.createNonExistingSubfolder = false;
			Directory.Delete(this.SubfolderAbspath);
			return ret;
		}
		public RepositoryBarsFile DataFileForSymbol(string symbol, bool throwIfDoesntExist = true) {
			RepositoryBarsFile barsFile = new RepositoryBarsFile(this, symbol, throwIfDoesntExist);
			return barsFile;
		}
	}
}