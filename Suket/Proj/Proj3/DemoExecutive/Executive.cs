///////////////////////////////////////////////////////////////////////
// Executive.cs - Demonstrate Prototype Code Analyzer                //
// ver 2.0                                                           //
// Language:    C#, 2017, .Net Framework 4.7.1                       //
// Platform:    Dell Precision T8900, Win10                          //
// Application: Demonstration for CSE681, Project #3, Fall 2018      //
// Author:      Jim Fawcett, CST 4-187, Syracuse University          //
//              (315) 443-3948, jfawcett@twcny.rr.com                //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package defines the class:
 *   Executive:
 *   - uses Parser, RulesAndActions, Semi, and Toker to perform type-based
 *     dependency analyzes
 */
/* Required Files:
 *   Executive.cs
 *   FileMgr.cs
 *   Parser.cs
 *   IRulesAndActions.cs, RulesAndActions.cs, ScopeStack.cs, Elements.cs
 *   ITokenCollection.cs, Semi.cs, Toker.cs
 *   Display.cs
 *   CsGraph.cs
 *   
 * Maintenance History:
 * --------------------
 * ver 2.0 : 29 Nov 2018
 * - added dependency and strong component analysis
 * ver 1.0 : 09 Oct 2018
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CodeAnalysis
{
	using Lexer;
	using CsGraph;

	///////////////////////////////////////////////////////////////////
	// Executive class
	// - finds files to analyze, using Navigate component
	// - builds typetable, in pass #1, by parsing files for defined types
	// - builds dependency table, in pass #2, by parsing files for:
	//   - type declarations, e.g., T t;, after stripping off modifiers
	//   - method parameter declarations, e.g., myFun(T t)
	//   - inheritance, e.g., class X : Y { ...
	//   and using typetable file and namespace info
	// - builds dependency graph from dependency table and analyzes 
	//   strong components

	public class Executive
	{
		List<string> files { get; set; } = new List<string>();

		//----< process commandline to verify path >---------------------

		bool ProcessCommandline(string[] args)
		{
			if (args.Length < 2)
			{
				Console.Write("\n  Please enter path to analyze\n\n");
				return false;
			}
			string path = args[0];
			if (!Directory.Exists(path))
			{
				Console.Write("\n  invalid path \"{0}\"", System.IO.Path.GetFullPath(path));
				return false;
			}
			return true;
		}
		//----< show arguments on command line >-------------------------

		static void ShowCommandLine(string[] args)
		{
			Console.Write("\n  Commandline args are:\n  ");
			foreach (string arg in args)
			{
				Console.Write("  {0}", arg);
			}
			Console.Write("\n  current directory: {0}", System.IO.Directory.GetCurrentDirectory());
			Console.Write("\n");
		}
		//----< build type table by parsing files for type defs >--------

		void typeAnalysis(List<string> files)
		{
			Console.Write("\n  Type Analysis");
			Console.Write("\n ---------------");

			Console.Write(
				"\n  {0,10}  {1,25}  {2,25}",
				"category", "name", "file"
			);
			Console.Write(
				"\n  {0,10}  {1,25}  {2,25}",
				"--------", "----", "----"
			);

			ITokenCollection semi = Factory.create();
			BuildTypeAnalyzer builder = new BuildTypeAnalyzer(semi);
			Parser parser = builder.build();
			Repository repo = Repository.getInstance();

			foreach (string file in files)
			{
				if (file.Contains("TemporaryGeneratedFile"))
					continue;
				if (!semi.open(file as string))
				{
					//Console.Write("\n  Can't open {0}\n\n", args[0]);
					continue;
				}

				repo.currentFile = file;
				repo.locations.Clear();

				try
				{
					while (semi.get().Count > 0)
						parser.parse(semi);
				}
				catch (Exception ex)
				{
					Console.Write("\n\n  {0}\n", ex.Message);
				}
				Repository rep = Repository.getInstance();
				List<Elem> table = rep.locations;
				Display.showMetricsTable(table);
				Console.Write("\n");

				semi.close();
			}
		}

		void TypeAnalys(List<string> files, string displaytokenfile, bool check)
		{
			if (check)
			{
				Console.Write("\n  Type Analysis");
				Console.Write("\n ---------------");

				Console.Write(
					"\n  {0,10}  {1,25}  {2,25}",
					"category", "name", "file"
				);
				Console.Write(
					"\n  {0,10}  {1,25}  {2,25}",
					"--------", "----", "----"
				);
			}
			ITokenCollection semi = Factory.create();
			BuildTypeAnalyzer builder = new BuildTypeAnalyzer(semi);
			Parser parser = builder.build();
			Repository repo = Repository.getInstance();

			foreach (string file in files)
			{
				if (file.Contains("TemporaryGeneratedFile"))
					continue;
				if (!semi.open(file as string))
				{
					//Console.Write("\n  Can't open {0}\n\n", args[0]);
					continue;
				}

				repo.currentFile = file;
				repo.locations.Clear();

				try
				{
					while (semi.get().Count > 0)
						parser.parse(semi);
				}
				catch (Exception ex)
				{
					Console.Write("\n\n  {0}\n", ex.Message);
				}
				Repository rep = Repository.getInstance();
				List<Elem> table = rep.locations;
				if (check)
				{
					Display.showMetricsTable(table);
					Console.Write("\n");
					TypeAnalysWrittenFile(displaytokenfile, table);
				}
				semi.close();
			}
		}

		private void TypeAnalysWrittenFile(string displaytokenfile, List<Elem> table)
		{
			if (!File.Exists(displaytokenfile))
			{
				File.Create(displaytokenfile);
				TextWriter wr = new StreamWriter(displaytokenfile);
				wr.WriteLine("category \t name \t file");

				foreach (Elem e in table)
				{
					string fileName = System.IO.Path.GetFileName(e.file);
					wr.WriteLine( e.type +"\t" +e.name + "\t" +fileName);
				}
				wr.Close();
			}
			else if (File.Exists(displaytokenfile))
			{
				using (var wr = new StreamWriter(displaytokenfile, false))
				{
					wr.WriteLine("category \t name \t file");
					foreach (Elem e in table)
					{
						string fileName = System.IO.Path.GetFileName(e.file);
						wr.WriteLine(e.type + "\t" + e.name + "\t" + fileName);
					}
				}
			}

		}

		public void displaytok(List<string> arguments, string displaytokenfile)
		{
			ITokenCollection semi = Factory.create();
			BuildTypeAnalyzer builder = new BuildTypeAnalyzer(semi);
			Parser parser = builder.build();
			Repository repo = Repository.getInstance();

			foreach (string file in arguments)
			{
				List<string> Tokencollection = new List<string>();
				if (file.Contains("TemporaryGeneratedFile"))
					continue;
				if (!semi.open(file as string))
				{
					//Console.Write("\n  Can't open {0}\n\n", args[0]);
					continue;
				}

				repo.currentFile = file;
				repo.locations.Clear();

				try
				{
					while (semi.get().Count > 0)
					{

						foreach (var toker in semi)
						{
							Console.WriteLine(toker);
							Tokencollection.Add(toker);
						}
					}
					parser.parse(semi);
				}
				catch (Exception ex)
				{
					Console.Write("\n\n  {0}\n", ex.Message);
				}
				WriteFile(displaytokenfile, Tokencollection);
			}
		}

		private void WriteFile(string displaytokenfile, List<string> tokencollection)
		{
			if (!File.Exists(displaytokenfile))
			{
				File.Create(displaytokenfile);
				TextWriter wr = new StreamWriter(displaytokenfile);
				foreach (string toker in tokencollection)
				{
					wr.WriteLine(toker);
				}
				wr.Close();
			}
			else if (File.Exists(displaytokenfile))
			{
				using (var wr = new StreamWriter(displaytokenfile, false)) 
				{
					foreach (string toker in tokencollection)
					{
						wr.WriteLine(toker);
					}
				}
			}
		}


		//----< build dependency table by parsing for type usage >-------

		void DependencyAnalys(List<string> files, string displaytokenfile, bool check)
		{
			Repository repo = Repository.getInstance();
			ITokenCollection semi = Factory.create();
			BuildDepAnalyzer builder2 = new BuildDepAnalyzer(semi);
			Parser parser = builder2.build();
			repo.locations.Clear();

			foreach (string file in files)
			{
				//Console.Write("\n  file: {0}", file);
				if (file.Contains("TemporaryGeneratedFile") || file.Contains("AssemblyInfo"))
					continue;

				if (!semi.open(file as string))
				{
					Console.Write("\n  Can't open {0}\n\n", file);
					break;
				}
				List<string> deps = new List<string>();
				repo.dependencyTable.addParent(file);

				repo.currentFile = file;

				try
				{
					while (semi.get().Count > 0)
					{
						//semi.show();
						parser.parse(semi);
					}
				}
				catch (Exception ex)
				{
					Console.Write("\n\n  {0}\n", ex.Message);
				}
			}
			if(check)
			{
				repo.dependencyTable.show();
				repo.dependencyTable.DependencyAnlyswrite(displaytokenfile);
			}
		}

		void dependencyAnalysis(List<string> files)
		{
			Repository repo = Repository.getInstance();
			ITokenCollection semi = Factory.create();
			BuildDepAnalyzer builder2 = new BuildDepAnalyzer(semi);
			Parser parser = builder2.build();
			repo.locations.Clear();

			foreach (string file in files)
			{
				//Console.Write("\n  file: {0}", file);
				if (file.Contains("TemporaryGeneratedFile") || file.Contains("AssemblyInfo"))
					continue;

				if (!semi.open(file as string))
				{
					Console.Write("\n  Can't open {0}\n\n", file);
					break;
				}
				List<string> deps = new List<string>();
				repo.dependencyTable.addParent(file);

				repo.currentFile = file;

				try
				{
					while (semi.get().Count > 0)
					{
						//semi.show();
						parser.parse(semi);
					}
				}
				catch (Exception ex)
				{
					Console.Write("\n\n  {0}\n", ex.Message);
				}
			}
		}

		public void displaytypeanalysis(List<string> arguments, string displaytokenfile)
		{
			TypeAnalys(arguments, displaytokenfile, true);
		}

		public void displaysemi(List<string> arguments, string displaytokenfile)
		{
			ITokenCollection semi = Factory.create();
			BuildTypeAnalyzer builder = new BuildTypeAnalyzer(semi);
			Parser parser = builder.build();
			Repository repo = Repository.getInstance();

			foreach (string file in arguments)
			{
				List<string> Semicollection = new List<string>();
				if (file.Contains("TemporaryGeneratedFile"))
					continue;
				if (!semi.open(file as string))
				{
					//Console.Write("\n  Can't open {0}\n\n", args[0]);
					continue;
				}

				repo.currentFile = file;
				repo.locations.Clear();

				try
				{
					while (semi.get().Count > 0)
					{

						Console.WriteLine(semi.ToString());
						Semicollection.Add(semi.ToString());
					}
					parser.parse(semi);
				}
				catch (Exception ex)
				{
					Console.Write("\n\n  {0}\n", ex.Message);
				}
				WriteFile(displaytokenfile, Semicollection);
			}
		}

		//----< build dependency graph from dependency table >-----------

		CsGraph<string, string> buildDependencyGraph()
		{
			Repository repo = Repository.getInstance();

			CsGraph<string, string> graph = new CsGraph<string, string>("deps");
			foreach (var item in repo.dependencyTable.dependencies)
			{
				string fileName = item.Key;
				fileName = System.IO.Path.GetFileName(fileName);

				CsNode<string, string> node = new CsNode<string, string>(fileName);
				graph.addNode(node);
			}

			DependencyTable dt = new DependencyTable();
			foreach (var item in repo.dependencyTable.dependencies)
			{
				string fileName = item.Key;
				fileName = System.IO.Path.GetFileName(fileName);
				if (!dt.dependencies.ContainsKey(fileName))
				{
					List<string> deps = new List<string>();
					dt.dependencies.Add(fileName, deps);
				}
				foreach (var elem in item.Value)
				{
					string childFile = elem;
					childFile = System.IO.Path.GetFileName(childFile);
					dt.dependencies[fileName].Add(childFile);
				}
			}

			foreach (var item in graph.adjList)
			{
				CsNode<string, string> node = item;
				List<string> children = dt.dependencies[node.name];
				foreach (var child in children)
				{
					int index = graph.findNodeByName(child);
					if (index != -1)
					{
						CsNode<string, string> dep = graph.adjList[index];
						node.addChild(dep, "edge");
					}
				}
			}
			return graph;
		}

		public void displaystrongcomponent(List<string> arguments, string displaytokenfile)
		{
			Repository repo = Repository.getInstance();
			TypeAnalys(arguments, displaytokenfile, false);
			DependencyAnalys(arguments, displaytokenfile, false);
			CsGraph<string, string> graph = buildDependencyGraph();
			graph.strongComponents();

			if (!File.Exists(displaytokenfile))
			{
				File.Create(displaytokenfile);
				TextWriter wr = new StreamWriter(displaytokenfile);		
				foreach (var item in graph.strongComp)
				{
					Console.Write("\n  component {0}", item.Key);
					Console.Write("\n    ");
					wr.Write("\n  component {0}", item.Key);
					wr.Write("\n    ");
					foreach (var elem in item.Value)
					{
						Console.Write("{0} ", elem.name);
						wr.Write("{0} ", elem.name);
					}
				}
				wr.Close();
			}
			else if (File.Exists(displaytokenfile))
			{
				using (var wr = new StreamWriter(displaytokenfile, false))
				{
					foreach (var item in graph.strongComp)
					{
						Console.Write("\n  component {0}", item.Key);
						Console.Write("\n    ");
						wr.Write("\n  component {0}", item.Key);
						wr.Write("\n    ");
						foreach (var elem in item.Value)
						{
							Console.Write("{0} ", elem.name);
							wr.Write("{0} ", elem.name);
						}
					}
				}
			}
		}

		
		public void displaydependency(List<string> arguments, string displaytokenfile)
		{
			Console.Write("\n Dependency Analysis Output:");
			Repository repo = Repository.getInstance();
			TypeAnalys(arguments, displaytokenfile, false);
			DependencyAnalys(arguments, displaytokenfile, true);
		}

		//----< processing starts here >---------------------------------

		static void Main(string[] args)
		{
			Console.Write("\n  Dependency Analysis");
			Console.Write("\n =====================\n");

			Executive exec = new Executive();

			ShowCommandLine(args);

			// finding files to analyze

			FileUtilities.Navigate nav = new FileUtilities.Navigate();
			nav.Add("*.cs");
			nav.go(args[0]);  // read path from command line
			List<string> files = nav.allFiles;

			exec.typeAnalysis(files);

			Console.Write("\n  TypeTable Contents:");
			Console.Write("\n ---------------------");

			Repository repo = Repository.getInstance();
			repo.typeTable.show();
			Console.Write("\n");

			/////////////////////////////////////////////////////////////////
			// Pass #2 - Find Dependencies

			Console.Write("\n  Dependency Analysis:");
			Console.Write("\n ----------------------");

			exec.dependencyAnalysis(files);
			repo.dependencyTable.show();

			Console.Write("\n\n  building dependency graph");
			Console.Write("\n ---------------------------");

			CsGraph<string, string> graph = exec.buildDependencyGraph();
			graph.showDependencies();

			Console.Write("\n\n  Strong Components:");
			Console.Write("\n --------------------");
			graph.strongComponents();
			foreach (var item in graph.strongComp)
			{
				Console.Write("\n  component {0}", item.Key);
				Console.Write("\n    ");
				foreach (var elem in item.Value)
				{
					Console.Write("{0} ", elem.name);
				}
			}
			Console.Write("\n\n");
		}
	}
}
