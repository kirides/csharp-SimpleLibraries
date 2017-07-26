using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Kirides.Libs.Extensions.IQueryable.Search
{
    [TestClass]
    public class IQueryableSearchExtensionsTest
    {
        private class PersonWithMessage
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Message { get; set; }
        }

        IQueryable<PersonWithMessage> data = new List<PersonWithMessage>
        {
            new PersonWithMessage{ Id = 1, Name = "Olaf", Message = "Hello World !" },
            new PersonWithMessage{ Id = 1, Name = "Sven", Message = "Good Evening !" },
            new PersonWithMessage{ Id = 1, Name = "Rick", Message = "Servus !" },
            new PersonWithMessage{ Id = 1, Name = "Morty", Message = "Good Day !" },
            new PersonWithMessage{ Id = 1, Name = "Leyla", Message = "Hey Olaf !" },
            new PersonWithMessage{ Id = 1, Name = "Lilly", Message = "Why coding is not bad practice" }
            ,new PersonWithMessage{ Id = 1, Name = "Marvin", Message = "Why coding is not so bad" },
            new PersonWithMessage{ Id = 1, Name = "Steve", Message = "Stackoverflow flows not so bad" }
            ,new PersonWithMessage{ Id = 1, Name = "Pickle", Message = "Hey Stackoverflow is bad Olaf, is not it?" },
            new PersonWithMessage{ Id = 1, Name = "Norman", Message = "Good is not bad" },
            new PersonWithMessage{ Id = 1, Name = "Lorem", Message = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et" },
            new PersonWithMessage{ Id = 1, Name = "Lorem", Message = "Lorem ipsum dolor sit amet, Blablabla aliquyam erat, sed diam voluptua. At vero eos et accusam et" },
            new PersonWithMessage{ Id = 1, Name = "Lorem", Message = "Lorem ipsum dolor sit amet, sed diam voluptua." },
            new PersonWithMessage{ Id = 1, Name = "Lorem", Message = "Lorem sit amet" },
            new PersonWithMessage{ Id = 1, Name = "Lorem", Message = "Lorem ipsum dolor sit amet, erat, sed diam voluptua." },
            new PersonWithMessage{ Id = 1, Name = "Lorem", Message = "Lorem ipsum dolor sit amet, Blablabla aliquyam erat, sed diam voluptua. At vero eos et accusam et" },
            new PersonWithMessage{ Id = 1, Name = "Ipsum", Message = "Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis." },

        }.AsQueryable();

        [TestMethod]
        public void FullTextSearchSingleWord()
        {
            var result = data.FullTextSearch("Olaf").ToList();
            Assert.IsTrue(result.Count == 3);
        }

        [TestMethod]
        public void FullTextSearchTwoWords()
        {
            var result = data.FullTextSearch("Olaf Hey").ToList();
            Assert.IsTrue(result.Count == 2);
        }

        [TestMethod]
        public void FullTextSearchThreeWords()
        {
            var result = data.FullTextSearch("not so bad").ToList();
            Assert.IsTrue(result.Count == 2);
        }

        [TestMethod]
        public void FullTextSearchFiveWords()
        {
            var result = data.FullTextSearch("Lorem ipsum dolor sit amet").ToList();
            Assert.IsTrue(result.Count == 5);
        }

        [TestMethod]
        public void FullTextSearchEightWords()
        {
            var result = data.FullTextSearch("Lorem ipsum dolor sit amet erat sed diam").ToList();
            Assert.IsTrue(result.Count == 4);
        }

        [TestMethod]
        public void FullTextSearchInsideVariable()
        {
            var result = data.FullTextSearch(x=>x.Message, "Good !").ToList();
            Assert.IsTrue(result.Count == 2);
        }

    }
}
