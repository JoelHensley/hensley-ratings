namespace TeamsParser;

static class KnownMappings
{
    // Maps raw section headers to canonical division names.
    public static readonly Dictionary<string, string> DivisionAliases = new()
    {
        { "NCAA Division I - Football Bowl Subdivision",          "FBS"         },
        { "NCAA Division I - Football Championship Subdivision",  "FCS"         },
        { "NCAA Division II",                                     "Division-II" },
        { "NCAA Division III",                                    "Division-III"},
        { "NAIA",                                                 "NAIA"        },
        { "Other",                                                "Other"       },
    };

    // Maps raw conference names to canonical names from the previous year's data.
    // Add entries here when a conference was renamed or abbreviated differently.
    public static readonly Dictionary<string, string> ConferenceAliases = new()
    {
        // FBS abbreviations
        { "Atlantic Coast Conference",    "ACC"    },
        { "Big 10 Conference",            "Big 10" },
        { "Big 12 Conference",            "Big 12" },
        { "Mountain West Conference",     "MWC"    },
        { "Southeastern Conference",      "SEC"    },
        { "Division I FBS Independent",   "Division I-A Independents"  },

        // FCS renames/mergers
        { "Coastal Athletic Association", "Colonial Athletic Association"     }, // CAA rebrand 2023
        { "NEC",                          "Northeast Conference"              },
        { "Ohio Valley Conference",       "Big South-Ohio Valley Conference"  }, // 2024 merger
        { "FCS Independents",             "Division I-AA Independents"        },

        // FCS renames (continued)
        { "Conference of New England",    "Commonwealth Coast Football"       }, // rebrand
        { "Landmark Conference",          "Landmark"                          }, // dropped "Conference"

        // Division-II
        { "Division II Independent",      "Division II Independents"          },

        // NAIA
        { "NAIA Independent",             "NAIA Independents"                 },
    };

    // Maps raw team names to canonical names from the previous year's data.
    // Add entries here when a team was renamed or spelled differently.
    public static readonly Dictionary<string, string> TeamAliases = new()
    {
        // Name differences from website export vs. canonical file
        { "Cornell",                "Cornell NY"               },
        { "LIU",                    "LIU-Post"                 },
        { "Cal Lutheran",           "California Lutheran"      },
        { "Claremont-Mdud-Scripps", "Claremont-Mudd-Scripps"  }, // typo in source
        { "Ottawa KS",              "Ottawa"                   },
        { "St Thomas MN",           "St Thomas"                },
        { "Maryville TN",           "Maryville"                },
        { "West Liberty",           "West Liberty St"          },
        { "Western Colorado",       "Western St CO"            },
        { "Wheeling U.",            "Wheeling Jesuit"          },
        { "Midland U.",             "Midland Lutheran"         },
        { "Point U.",               "Point"                    },
        { "Bluefield VA",           "Bluefield"                },
        { "Washington MO",          "Washington U."            },
        { "East Texas A&M",         "TAMU-Commerce"            },

        // Typos / abbreviated forms in the website export
        { "Northeasthern St OK",    "Northeastern St OK"       }, // typo in source
        { "Colorado Mesa",          "Mesa St"                  },
        { "Lewis & Clark OR",       "Lewis & Clark"            },
        { "Georgetown",             "Georgetown KY"            }, // disambiguate from Georgetown DC
        { "Nelson TX",              "Nelson U."                },
        { "Castleton",              "Castleton St"             },
    };
}
