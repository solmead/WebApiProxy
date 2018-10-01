var Tasks;
(function (Tasks) {
    var Task = /** @class */ (function () {
        function Task(func) {
            //super((resolve, reject) => {
            //        resolveFunc = resolve;
            //});
            var _this = this;
            this.func = func;
            this.promise = null;
            this.then = function (onFulfilled) {
                return _this.promise.then(onFulfilled);
            };
            this.start = function () {
                _this.func(function (val) {
                    _this.resolveFunc(val);
                });
            };
            this.promise = new Promise(function (resolve) {
                _this.resolveFunc = resolve;
            });
            if (!this.func) {
                this.func = function (rFunc) {
                    return rFunc();
                };
            }
            else if (func.length === 0) {
                var bfunc = this.func;
                this.func = function (rFunc) {
                    bfunc();
                    rFunc();
                };
            }
        }
        return Task;
    }());
    Tasks.Task = Task;
    var RecurringTask = /** @class */ (function () {
        function RecurringTask(callback, timeout, maxLockTime) {
            var _this = this;
            this.callback = callback;
            this.timeout = timeout;
            this.maxLockTime = maxLockTime;
            this._isRunning = false;
            this.locker = new Lock.Locker();
            this.timedCall = function () {
                if (!_this.isLocked() && _this.callback) {
                    _this.callback();
                }
                if (_this.isRunning) {
                    setTimeout(function () { _this.timedCall(); }, _this.timeout);
                }
            };
            //private set isRunning(value: boolean) {
            //    this._isRunning = value;
            //}
            this.setTimeOut = function (time) {
                _this.timeout = time;
            };
            this.lock = function () {
                _this.locker.lock();
            };
            this.unLock = function () {
                _this.locker.unLock();
            };
            this.isLocked = function () {
                return _this.locker.isLocked();
            };
            this.start = function () {
                if (!_this.isRunning) {
                    _this._isRunning = true;
                    _this.timedCall();
                }
            };
            this.stop = function () {
                _this._isRunning = false;
            };
        }
        Object.defineProperty(RecurringTask.prototype, "isRunning", {
            get: function () {
                return this._isRunning;
            },
            enumerable: true,
            configurable: true
        });
        return RecurringTask;
    }());
    Tasks.RecurringTask = RecurringTask;
    function runAfterWait(waitTimeMilliSeconds) {
        var t = new Task(function (cback) {
            cback();
        });
        var timer = null;
        var throttle = function () {
            clearTimeout(timer);
            timer = window.setTimeout(function () {
                t.start();
            }, waitTimeMilliSeconds || 500);
        };
        t.trigger = function () {
            throttle();
        };
        t.call = function () {
            clearTimeout(timer);
            t.start();
        };
        return t;
    }
    Tasks.runAfterWait = runAfterWait;
    function debounced() {
        var t = new Task(function (cback) {
            cback();
        });
        t.trigger = function () {
            t.start();
        };
        t.call = function () {
            t.start();
        };
        return t;
    }
    Tasks.debounced = debounced;
    //export function debouncedAtEnd(waitTimeMilliSeconds: number): IDebouncedTask<void> {
    //    var t = new Task<void>((cback) => {
    //        setTimeout(cback, waitTimeMilliSeconds);
    //    }) as IDebouncedTask<void>;
    //    t.trigger = (): void => {
    //        t.start();
    //    }
    //    t.call = (): void => {
    //        t.start();
    //    }
    //    return t;
    //}
    function delay(msec) {
        return new Promise(function (resolve) {
            setTimeout(resolve, msec);
        });
    }
    Tasks.delay = delay;
    function whenReady() {
        return new Promise(function (resolve) {
            $(function () {
                resolve();
            });
        });
    }
    Tasks.whenReady = whenReady;
    function whenTrue(trueFunc) {
        if (!trueFunc || trueFunc()) {
            return new Promise(function (resolve) {
                resolve();
            });
        }
        return new Promise(function (resolve) {
            var obj = new RecurringTask(function () {
                obj.lock();
                if (trueFunc()) {
                    resolve();
                    obj.stop();
                }
                obj.unLock();
            }, 100);
            obj.start();
        });
    }
    Tasks.whenTrue = whenTrue;
})(Tasks || (Tasks = {}));
//# sourceMappingURL=Tasks.js.map