using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using System.Diagnostics;


/// <summary>
/// A tool to search through a csv file of legislative bills {Title, Description, Keywords, Legislative Session, Senators/Intro Committee, Representatives, Committee}
/// and generate a new csv file with the search results, based on the user's criteria, with the option to generate committee statistics,
/// with a user interface to select the csv file to search through and the criteria to search by
/// </summary>
/// <Authors>John Dawson, Wisconsin State Legislature, 2024</Authors>
namespace BillSearchTool
{

    public class MainForm : Form
    {
        //File path textboxes
        private TextBox filePathTextBox;
        private Button openFileButton;
        private TextBox outputFolderTextBox;
        private Button selectOutputFolderButton;

        //Search criteria textboxes
        private TextBox titleTextBox;
        private TextBox keywordsTextBox;
        private TextBox senatorsIntroCommitteesTextBox;
        private TextBox representativesTextBox;
        private TextBox senatorAuthorTextBox;
        private TextBox representativeAuthorTextBox;
        private TextBox committeeTextBox;

        //Search button
        private Button searchButton;
        private TextBox resultsTextBox;

        //Bill search object
        private BillSearch? billSearch;

        //Main form constructor
        public MainForm()
        {
            InitializeComponents();
        }

        //Initialize the components of the main form
        private void InitializeComponents()
        {
            this.Text = "Bill Search Tool";
            this.Width = 800;
            this.Height = 800;
            this.Padding = new Padding(10);

            // Main panel
            var mainPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            this.Controls.Add(mainPanel);

            // File path section
            var filePathPanel = CreateLabeledInput("Selected .CSV (Excel) File: (With Title, Description, Keywords... format)", out filePathTextBox, out openFileButton, "Open File");
            openFileButton.Click += OpenFileButton_Click;
            mainPanel.Controls.Add(filePathPanel);

            // Output folder section
            var outputFolderPanel = CreateLabeledInput("Output Folder (where do you want the results to be put?):", out outputFolderTextBox, out selectOutputFolderButton, "Select Folder");
            selectOutputFolderButton.Click += SelectOutputFolderButton_Click;
            mainPanel.Controls.Add(outputFolderPanel);

            // Search criteria sections
            mainPanel.Controls.Add(CreateLabeledTextbox("Title (such as '2023 Senate Bill 1'):", out titleTextBox));
            mainPanel.Controls.Add(CreateLabeledTextbox("Keywords (separate with commas, such as 'Drugs, Physician,...'):", out keywordsTextBox));
            mainPanel.Controls.Add(CreateLabeledTextbox("Senators/Intro Committees (separate with commas, such as 'Larson' or 'Larson, Carpenter, ...'):", out senatorsIntroCommitteesTextBox));
            mainPanel.Controls.Add(CreateLabeledTextbox("Representatives (separate with commas, such as 'Sinicki' or 'Sinicki, Andraca, ...'):", out representativesTextBox));
            mainPanel.Controls.Add(CreateLabeledTextbox("Senate Author (such as 'Larson' or 'Joint Legislative Council'):", out senatorAuthorTextBox));
            mainPanel.Controls.Add(CreateLabeledTextbox("Representative Author (such as 'Sinicki'):", out representativeAuthorTextBox));
            mainPanel.Controls.Add(CreateLabeledTextbox("Committee (such as 'Committee on Universities and Revenue'):", out committeeTextBox));

            // Search button
            searchButton = new Button
            {
                Text = "Search",
                Width = 100,
                Height = 30,
                Margin = new Padding(10)
            };
            searchButton.Click += SearchButton_Click;
            mainPanel.Controls.Add(searchButton);

            // Results textbox
            resultsTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Width = 750,
                Height = 200,
                Margin = new Padding(10)
            };
            mainPanel.Controls.Add(resultsTextBox);
        }


        //Create a labeled textbox
        private Panel CreateLabeledTextbox(string labelText, out TextBox textBox)
        {
            var panel = new Panel
            {
                Width = 750,
                Height = 70, // Increased height to accommodate stacked layout
                Margin = new Padding(10)
            };

            var label = new Label
            {
                Text = labelText,
                Width = 700, // Allow label to span full width for clarity
                Height = 20,
                Left = 0,
                Top = 0 // Top aligned
            };
            panel.Controls.Add(label);

            textBox = new TextBox
            {
                Left = 0,
                Top = 25, // Positioned below the label
                Width = 700 // Spanning full panel width
            };
            panel.Controls.Add(textBox);

            return panel;
        }

        //Create a labeled input
        private Panel CreateLabeledInput(string labelText, out TextBox textBox, out Button button, string buttonText)
        {
            var panel = new Panel
            {
                Width = 750,
                Height = 90, // Increased height to accommodate stacked layout with button
                Margin = new Padding(10)
            };

            var label = new Label
            {
                Text = labelText,
                Width = 700,
                Height = 20,
                Left = 0,
                Top = 0
            };
            panel.Controls.Add(label);

            textBox = new TextBox
            {
                Left = 0,
                Top = 25, // Positioned below the label
                Width = 540 // Space for the button on the same row
            };
            panel.Controls.Add(textBox);

            button = new Button
            {
                Text = buttonText,
                Left = 550, // Positioned to the right of the textbox
                Top = 25,
                Width = 120
            };
            panel.Controls.Add(button);

            return panel;
        }

        //Event handlers
        //Open a file dialog to select a csv file
        private void OpenFileButton_Click(object? sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.DefaultExt = ".csv";
                openFileDialog.Filter = "CSV files (*.csv)|*.csv|All Files (*.*)|*.*";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePathTextBox.Text = openFileDialog.FileName;
                    billSearch = new BillSearch(openFileDialog.FileName);
                    MessageBox.Show("File loaded successfully!");
                }
            }
        }

        //Open a folder dialog to select the output folder
        private void SelectOutputFolderButton_Click(object? sender, EventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select the folder that you want to save the search results (as an excel file of bills) to";

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    outputFolderTextBox.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        //Search for bills based on the user's criteria; event handler for the search button
        private void SearchButton_Click(object? sender, EventArgs e)
        {

            //check if a file has been loaded
            if (billSearch == null)
            {
                MessageBox.Show("Please load a file first.");
                return;
            }

            //check if the output folder is valid
            if (string.IsNullOrWhiteSpace(outputFolderTextBox.Text) || !Directory.Exists(outputFolderTextBox.Text))
            {
                MessageBox.Show("Please select a valid output folder.");
                return;
            }

            //the user's criteria defined below:
            var criteria = new Dictionary<string, List<string>>
            {
                { "Title", new List<string> { titleTextBox.Text } },
                { "Keywords", new List<string> { keywordsTextBox.Text } },
                { "SenatorsIntroCommittee", new List<string> { senatorsIntroCommitteesTextBox.Text } },
                { "Representatives", new List<string> { representativesTextBox.Text } },
                { "SenatorAuthor", new List<string> { senatorAuthorTextBox.Text } },
                { "RepresentativeAuthor", new List<string> { representativeAuthorTextBox.Text } },
                { "Committee", new List<string> { committeeTextBox.Text } }
            };

            
            string outputFilePath = Path.Combine(outputFolderTextBox.Text, "TEMPORARYsearchResults.csv");

            try
            {
                //if the search results are found, save them to a new csv file
                string? resultFilePath = billSearch.SearchByCriteria(criteria, outputFilePath);

                //if the search results are found, display the results file path
                if (resultFilePath != null)
                {
                    resultsTextBox.Text = $"Search results saved to: \n{resultFilePath}";
                }
                //if no search results are found, display a message
                else
                {
                    resultsTextBox.Text = "No matching results found.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }
    }




    //Map the csv file to the BillRecord class
    public class BillRecordMap : ClassMap<BillRecord>
    {
        public BillRecordMap()
        {
            Map(m => m.Title).Name("Title");
            Map(m => m.Description).Name("Description");
            Map(m => m.Keywords).Name("Keywords");
            Map(m => m.LegislativeSession).Name("Legislative Session");
            Map(m => m.SenatorsIntroCommittee).Name("Senators/Intro Committee");
            Map(m => m.Representatives).Name("Representatives");
            Map(m => m.Committee).Name("Committee");
        }
    }



    //BillRecord class with the fields of the csv file, allowing for the bills to be mapped into a List<BillRecord>
    public class BillRecord
    {
        [Name("Title")]
        public string? Title { get; set; }
        [Name("Description")]
        public string? Description { get; set; }
        [Name("Keywords")]
        public string? Keywords { get; set; }
        [Name("Legislative Session")]
        public string? LegislativeSession { get; set; }
        [Name("Senators/Intro Committee")]
        public string? SenatorsIntroCommittee { get; set; }
        [Name("Representatives")]
        public string? Representatives { get; set; }
        [Name("Committee")]
        public string? Committee { get; set; }
    }


    //BillSearch class to search through the bills and generate a new csv file with the search results
    public class BillSearch
    {
        private List<BillRecord> billRecords;

        //BillSearch constructor to load the csv file
        public BillSearch(string filePath)
        {
            //load csv data
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null //ignore missing fields
            }))
            {
                billRecords = csv.GetRecords<BillRecord>().ToList();
            }
        }


        /// <summary>
        ///So, this method will search through the bills and save a new csv file with the search results
        ///The search results will be based on the user's criteria: for example, the user can search for bills with a specific title, description, legislative session, SenatorAuthor, RepresentativeAuthor, or committee
        ///For queries like keywords, senators/intro committee, and representatives, the user can search for multiple values in the later multisearch method
        /// </summary>
        /// <param name="criteria">a Dictionary<string, List<string>> criteria parameter</param>
        /// <returns>A List<BillRecord> class containing the bills that successfully passed the SingleSearch filter</returns>
      
        public List<BillRecord>? SingleSearch(Dictionary<string, List<string>> criteria)
        {
            Debug.WriteLine("Starting SingleSearch");

            // If no criteria relevant to SingleSearch are provided, return all bills.
            var singleSearchCriteria = new[] { "Title", "Description", "LegislativeSession", "Committee" };
            bool hasSingleSearchCriteria = criteria.Keys.Any(key => singleSearchCriteria.Contains(key));

            //if no single search criteria are provided, return all bills
            if (!hasSingleSearchCriteria)
            {
                //Debug.WriteLine("No SingleSearch criteria provided. Returning all bills.");
                return billRecords; // Return all bills to proceed to MultiSearch
            }

            //Filter bills based on the user's criteria
            var filteredBills = billRecords.Where(bill =>
            {
                bool matchesAllCriteria = true;

                foreach (var criterion in criteria)
                {
                    var fieldName = criterion.Key;
                    var searchTerms = criterion.Value;

                    //Debug.WriteLine($"Checking field: {fieldName}, with search terms: {string.Join(", ", searchTerms)}");

                    // Handle main fields for SingleSearch
                    bool fieldMatches = fieldName switch
                    {
                        "Title" => searchTerms.Any(term => bill.Title?.Contains(term, StringComparison.OrdinalIgnoreCase) == true),
                        "Description" => searchTerms.Any(term => bill.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) == true),
                        "LegislativeSession" => searchTerms.Any(term => bill.LegislativeSession?.Contains(term, StringComparison.OrdinalIgnoreCase) == true),
                        "Committee" => searchTerms.Any(term => bill.Committee?.Contains(term, StringComparison.OrdinalIgnoreCase) == true),
                        _ => true // Default to true for fields not handled here
                    };

                    //Debug.WriteLine($"Field: {fieldName}, Matches: {fieldMatches}");

                    // If any field does not match, the bill does not meet all criteria
                    if (!fieldMatches)
                    {
                        matchesAllCriteria = false;
                        break;
                    }
                }

                //Check 'SenatorAuthor' if present in criteria
                if (criteria.ContainsKey("SenatorAuthor"))
                {
                    bool senatorAuthorMatches = criteria["SenatorAuthor"].Any(term =>
                        !string.IsNullOrWhiteSpace(bill.SenatorsIntroCommittee) &&
                        bill.SenatorsIntroCommittee.Split(',')[0]?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);

                    Debug.WriteLine($"SenatorAuthor Matches: {senatorAuthorMatches}");

                    if (!senatorAuthorMatches)
                    {
                        matchesAllCriteria = false;
                    }
                }

                //Check 'RepresentativeAuthor' if present in criteria
                if (criteria.ContainsKey("RepresentativeAuthor"))
                {
                    //Check if the first representative matches the search term; if so, consider the bill a match, otherwise, exclude it
                    bool representativeAuthorMatches = criteria["RepresentativeAuthor"].Any(term =>
                        !string.IsNullOrWhiteSpace(bill.Representatives) &&
                        bill.Representatives.Split(',')[0]?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);

                    //Debug.WriteLine($"RepresentativeAuthor Matches: {representativeAuthorMatches}");

                    if (!representativeAuthorMatches)
                    {
                        matchesAllCriteria = false;
                    }
                }

                return matchesAllCriteria;
            }).ToList();

            //Debug.WriteLine($"Filtered bills count: {filteredBills.Count}");

            //Return filtered bills if any are found, otherwise return null
            return filteredBills.Any() ? filteredBills : null;
        }




        /// <summary>
        /// Search for bills that contain multiple queries
        /// </summary>
        /// <param name="criteria">A Dictionary<string, List<string>> criteria parameter</param>
        /// <param name="singleFilteredBills">A List<BillRecord> class containing the bills that successfully passed the SingleSearch filter</param>
        /// <returns>A List<BillRecord> class containing the bills that successfully passed both the SingleSearch and Multisearch filters</returns>
        public List<BillRecord>? MultiSearch(Dictionary<string, List<string>> criteria, List<BillRecord> singleFilteredBills)
        {
            //split the criteria into separate search terms lists:
            //keywords, senators / intro committee, and representatives
            var keywords = criteria.GetValueOrDefault("Keywords", new List<string>());
            var senators = criteria.GetValueOrDefault("SenatorsIntroCommittee", new List<string>());
            var representatives = criteria.GetValueOrDefault("Representatives", new List<string>());

            //split the keywords, senators, and representatives into their entries
            keywords = keywords.SelectMany(keyword => keyword.Split(',')).ToList(); //split the search terms by commas
            senators = senators.SelectMany(senator => senator.Split(',')).ToList(); //split the search terms by commas
            representatives = representatives.SelectMany(rep => rep.Split(',')).ToList();


            //filter the bills based on the user's criteria
            var multiFilteredBills = singleFilteredBills.Where(bill =>
            {
                bool matchesKeywords = !keywords.Any() || keywords.All(keyword =>
                    bill.Keywords?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true);

                bool matchesSenators = !senators.Any() || senators.All(senator =>
                    bill.SenatorsIntroCommittee?.Contains(senator, StringComparison.OrdinalIgnoreCase) == true);

                bool matchesRepresentatives = !representatives.Any() || representatives.All(rep =>
                    bill.Representatives?.Contains(rep, StringComparison.OrdinalIgnoreCase) == true);

                return matchesKeywords && matchesSenators && matchesRepresentatives;
            }).ToList();

            //return the filtered bills if any are found, otherwise return null
            return multiFilteredBills.Any() ? multiFilteredBills : null;
        }


        /// <summary>
        /// Search methods bound together, to search through the bills and generate a new csv file with the search results
        /// </summary>
        /// <param name="criteria">A Dictionary<string, List<string>> criteria parameter</param>
        /// <param name="outputFilePath">The outputFilePath where the search results will end up, as defined by the user</param>
        /// <returns>The outputFilePath where the search results ended up as a nullable string</returns>
        /// <exception cref="IOException">If there's any error saving the file</exception>
        public string? SearchByCriteria(Dictionary<string, List<string>> criteria, string outputFilePath)
        {
            //Perform initial filtering
            var singleFilteredBills = SingleSearch(criteria);
            //Debug.WriteLine("I've reached here! 1");
            if (singleFilteredBills == null || !singleFilteredBills.Any())
            {
                return null;
            }

            //Refine results
            var multiFilteredBills = MultiSearch(criteria, singleFilteredBills);
            //Debug.WriteLine("I've reached here! 2");
            if (multiFilteredBills == null || !multiFilteredBills.Any())
            {
                return null;
            }

            //Debug.WriteLine("I've reached here! 3");
            //Write results to CSV
            try
            {
                using var writer = new StreamWriter(outputFilePath);
                using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
                csv.WriteRecords(multiFilteredBills);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error writing search results to file: {ex.Message}");
            }

            return outputFilePath;
        }


        /// <summary>
        /// Generate statistics for each unique committee and save them to a new csv file, written as a vestigial method
        /// </summary>
        /// <param name="outputFile">Where does the user want the statistics for each committee outputted?</param>
        public void GenerateCommitteeStatistics(string outputFile = "committee_statistics.csv")
        {
            //calculate statistics for each unique committee
            var committeeStats = billRecords
                .GroupBy(bill => bill.Committee)
                .Select(group => new
                {
                    Committee = group.Key,
                    Count = group.Count()
                })
                .ToList();

            //output to a new csv
            using (var writer = new StreamWriter(outputFile))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.WriteRecords(committeeStats);
            }

            Debug.WriteLine("Committee statistics have been generated and saved to " + outputFile);
        }
    }


    /// <summary>
    /// The main class of the program, which runs the main form
    /// </summary>
    public class BillSearcherTool
    {
        [STAThread] //required for windows forms
        public static void Main(string[] args)
        {

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());

        }
    }
}