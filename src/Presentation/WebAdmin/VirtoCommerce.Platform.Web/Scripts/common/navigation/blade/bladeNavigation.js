angular.module('platformWebApp.bladeNavigation', [
])
.directive('vaBladeContainer', ['$compile', 'bladeNavigationService', function ($compile, bladeNavigationService)
{
    return {
        restrict: 'E',
        replace: true,
        templateUrl: 'Scripts/common/navigation/blade/bladeContainer.tpl.html',
        link: function (scope)
        {
            scope.blades = bladeNavigationService.stateBlades();
        }
    }
}])
.directive('vaBlade', ['$compile', 'bladeNavigationService', '$timeout', function ($compile, bladeNavigationService, $timeout)
{
    return {
        terminal: true,
        priority: 100,
        link: function (scope, element)
        {
            element.attr('ng-controller', scope.blade.controller);
            element.attr('id', scope.blade.id);
            element.attr('ng-model', "blade");
            element.removeAttr("va-blade");
            $compile(element)(scope);

            var speed = (window.navigator.platform == 'MacIntel' ? .5 : 40);

            $timeout(function ()
            {
                var mainContent = $('.main-content');
                var blade = $(element).parent('.blade');
                var offset = parseInt(blade.offset().left + mainContent.scrollLeft() + blade.width() - 85 - mainContent[0].clientWidth);
                if (offset > mainContent.scrollLeft()) {
                    mainContent.animate({ scrollLeft: offset + 'px' }, 500);
                }
            }, 500);


            //Somehow vertical scrollbar does not work initially so need to turn it on
            $(element).find('.blade-content').off('mousewheel').on('mousewheel', function (event, delta)
            {
                this.scrollTop -= (delta * speed);
                event.preventDefault();
            });

            $(element).off('mouseenter').on('mouseenter', function (e) {
                var blade = $(this).parents('.blade'),
                    bladeI = blade.find('.blade-inner'),
                    bladeH = bladeI.height(),
                    bladeIh = blade.find('.inner-block').height();

                if(blade.length) {
                    if(bladeH <= bladeIh) {
                        horizontalScroll('off');
                    }
                    else {
                        horizontalScroll('on');
                    }
                }
                else {
                    horizontalScroll('on');
                }
            });

            $('.blade-head, .blade-head *, .static, .static *').on('mouseenter', function (event) {
                horizontalScroll('on');
            });

            function horizontalScroll(flag)
            {
                if (flag != 'off')
                {
                    $('.main-content').off('mousewheel').on('mousewheel', function (event, delta)
                    {
                        this.scrollLeft -= (delta * speed);
                        event.preventDefault();
                    });
                }
                else
                {
                    $('.main-content').unmousewheel();
                }
            }

            scope.bladeMaximize = function ()
            {
                scope.maximized = true;

                var blade = $(element);
                blade.attr('data-width', blade.width());
                var leftMenu = $('.left-menu');
                var offset = parseInt(blade.offset().left + $('.main-content').scrollLeft() - leftMenu.width());
                var contentblock = blade.find(".blade-content");
                $(contentblock).animate({ width: (parseInt(window.innerWidth - leftMenu.width()) + 'px') }, 100);
                $('.main-content').animate({ scrollLeft: offset + 'px' }, 250);
            };

            scope.bladeRestore = function ()
            {
                scope.maximized = false;

                var blade = $(element);
                var blockWidth = blade.data('width');
                var leftMenu = $('.left-menu');
                blade.removeAttr('data-width');

                var offset = parseInt(blade.offset().left + $('.main-content').scrollLeft() - leftMenu.width());
                var contentblock = blade.find(".blade-content");
                $(contentblock).animate({ width: blockWidth }, 100);
                $('.main-content').animate({ scrollLeft: offset + 'px' }, 250);
            };

            scope.bladeClose = function ()
            {
                bladeNavigationService.closeBlade(scope.blade);
            };

            scope.executeCommand = function (toolbarCommand)
            {
                if (toolbarCommand.canExecuteMethod())
                    toolbarCommand.executeMethod();
            };
        }
    };
}])
.factory('bladeNavigationService', ['$rootScope', '$filter', '$state', function ($rootScope, $filter, $state)
{
    var service = {
        blades: [],
        currentBlade: undefined,
        closeBlade: function (blade, callback)
        {
            //Need in case a copy was passed
            blade = service.findBlade(blade.id);
            var idx = service.stateBlades().indexOf(blade);

            var doCloseBlade = function ()
            {
                service.stateBlades().splice(idx, 1);
                //remove blade from childs collection
                if (angular.isDefined(blade.parentBlade))
                {
                    var childIdx = blade.parentBlade.childrenBlades.indexOf(blade);
                    if (childIdx >= 0)
                    {
                        blade.parentBlade.childrenBlades.splice(childIdx, 1);
                    }
                }
                if (angular.isFunction(callback))
                {
                    callback();
                };
            };
            if (idx >= 0)
            {
                if (angular.isFunction(blade.onClose))
                {
                    blade.onClose(doCloseBlade);
                }
                else
                {
                    doCloseBlade();
                }
            }
        },
        hasBlade: function (id)
        {
            return service.findBlade(id) !== undefined;
        },
        stateBlades: function (stateName)
        {
            if (angular.isUndefined(stateName))
            {
                stateName = $state.current.name;
            }

            if (angular.isUndefined(service.blades[stateName]))
            {
                service.blades[stateName] = [];
            }

            return service.blades[stateName];
        },
        findBlade: function (id)
        {

            var found;
            angular.forEach(service.stateBlades(), function (blade)
            {
                if (blade.id == id)
                {
                    found = blade;
                }
            });

            return found;
        },
        showBlade: function (blade, parentBlade)
        {

            blade.isLoading = true;
            blade.parentBlade = parentBlade;
            blade.childrenBlades = [];

            var existingBlade = service.findBlade(blade.id);

            //Show blade in previous location
            if (existingBlade != undefined)
            {
                //store prev blade x-index
                blade.xindex = existingBlade.xindex;
            }

            //Show blade as last one by default
            if (!angular.isDefined(blade.xindex))
            {
                blade.xindex = service.stateBlades().length;
            }


            if (angular.isDefined(parentBlade))
            {
                blade.xindex = service.stateBlades().indexOf(parentBlade) + 1;
                parentBlade.childrenBlades.push(blade);
            }

            var showBlade = function ()
            {
                //show blade in same place where it been
                service.stateBlades().splice(Math.min(blade.xindex, service.stateBlades().length), 0, blade);
                service.currentBlade = blade;
            };

            if (existingBlade != undefined)
            {
                service.closeBlade(existingBlade, showBlade);
            }
            else
            {
                showBlade();
            }

        },
        setError: function(title, blade) {
            blade.error = {
                status: true,
                title: title
            };
        }
    };

    return service;
}]);

