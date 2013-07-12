define(['services/logger'], function (logger) {
    var availableTags = [
            { label: "ActionScript", value: 1 },
            { label: "AppleScript", value: 2 },
            { label: "Asp", value: 3 },
            { label: "BASIC", value: 4 },
            { label: "C", value: 5 },
            { label: "C++", value: 6 },
            { label: "Clojure", value: 7 },
            { label: "COBOL", value: 8 },
            { label: "ColdFusion", value: 9 },
            { label: "Erlang", value: 10 },
            { label: "Fortran", value: 11 },
            { label: "Groovy", value: 12 },
            { label: "Haskell", value: 13 },
            { label: "Java", value: 14 },
            { label: "JavaScript", value: 15 },
            { label: "Lisp", value: 16 },
            { label: "Perl", value: 17 },
            { label: "PHP", value: 18 },
            { label: "Python", value: 19 },
            { label: "Ruby", value: 20 },
            { label: "Scala", value: 21 },
            { label: "Scheme", value: 22 }
    ];

    var vm = function(){

        var self = this;
            
        self.activate = activate;
        self.title= 'Home View';

        self.langName = ko.observable();
        self.selectedLangs = ko.observableArray();
            
        // return some programming languages
        self.getLangs = function(){
            return availableTags;
        }
            
        // user clicked on a auto complete item
        self.addLang = function(event, ui){
                    
            $(event.target).val("");
                
            var lang = ui.item.label;
            var id = ui.item.value;
            
            self.selectedLangs.push("id: " + id + ", Language: " + lang);
                
            return false;
        }
                    
        return {
            getLangs : self.getLangs,
            addLang : self.addLang,
            langName : self.langName,
            selectedLangs: self.selectedLangs                        
        }
    }();

    return vm;

    //#region Internal Methods
    function activate() {
        logger.log('Home View Activated', null, 'home', true);
        return true;
    }
    //#endregion


});